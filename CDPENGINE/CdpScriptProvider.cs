namespace OrbKit_Quest_Completer.CDPENGINE;

public sealed class CdpScriptProvider
{
    public string ScriptsDirectory { get; }
    public string CustomScriptPath { get; }

    private DateTime _cachedWriteTimeUtc;
    private string? _cachedScript;
    private ScriptValidationResult? _cachedValidation;

    public CdpScriptProvider(string scriptsDirectory)
    {
        ScriptsDirectory = scriptsDirectory;
        CustomScriptPath = Path.Combine(scriptsDirectory, "cdp_patch.js");
        try { Directory.CreateDirectory(scriptsDirectory); } catch { }
    }

    public bool HasCustomFile => File.Exists(CustomScriptPath);

    public bool IsUsingValidCustomScript
    {
        get
        {
            _ = GetActiveScript(out _);
            return _cachedScript != null;
        }
    }

    public string ActiveVersion
    {
        get
        {
            _ = GetActiveScript(out _);
            return _cachedValidation?.Version ?? "1.0.0";
        }
    }

    public string ActiveShortHash
    {
        get
        {
            _ = GetActiveScript(out _);
            string hash = _cachedValidation?.Sha256 ?? string.Empty;
            return hash.Length >= 8 ? hash[..8] : "built-in";
        }
    }

    public string StatusSummary =>
        IsUsingValidCustomScript
            ? $"Script: Custom v{ActiveVersion} ({ActiveShortHash})"
            : $"Script: Built-in v{ActiveVersion}";

    public string GetActiveScript(out string? fallbackWarning)
    {
        fallbackWarning = null;

        if (!File.Exists(CustomScriptPath))
        {
            _cachedScript = null;
            _cachedValidation = CdpScriptValidator.ValidateStatic(DefaultPatchJs);
            return DefaultPatchJs;
        }

        DateTime writeTime = File.GetLastWriteTimeUtc(CustomScriptPath);
        if (_cachedValidation != null && writeTime == _cachedWriteTimeUtc)
        {
            if (_cachedScript != null)
                return _cachedScript;

            fallbackWarning = $"Rejected Scripts\\cdp_patch.js: {string.Join(" ", _cachedValidation.Errors)} (using built-in).";
            return DefaultPatchJs;
        }

        string raw = File.ReadAllText(CustomScriptPath);
        var validation = CdpScriptValidator.ValidateStatic(raw);
        _cachedWriteTimeUtc = writeTime;
        _cachedValidation = validation;

        if (!validation.IsValid)
        {
            _cachedScript = null;
            fallbackWarning = $"Rejected Scripts\\cdp_patch.js: {string.Join(" ", validation.Errors)} (using built-in).";
            return DefaultPatchJs;
        }

        _cachedScript = raw;
        return raw;
    }

    public ScriptValidationResult ImportValidatedContent(string scriptContent)
    {
        var result = CdpScriptValidator.ValidateStatic(scriptContent);
        if (!result.IsValid)
            return result;

        Directory.CreateDirectory(ScriptsDirectory);
        File.WriteAllText(CustomScriptPath, scriptContent);
        _cachedScript = scriptContent;
        _cachedValidation = result;
        _cachedWriteTimeUtc = File.GetLastWriteTimeUtc(CustomScriptPath);
        return result;
    }

    public void ExportTemplateTo(string destinationPath, bool builtInOnly = false)
    {
        string content = builtInOnly ? DefaultPatchJs : GetActiveScript(out _);
        File.WriteAllText(destinationPath, content);
    }

    public void ResetToDefault()
    {
        if (File.Exists(CustomScriptPath))
            File.Delete(CustomScriptPath);

        _cachedScript = null;
        _cachedWriteTimeUtc = default;
        _cachedValidation = CdpScriptValidator.ValidateStatic(DefaultPatchJs);
    }

    public const string DefaultPatchJs = """
        // OrbKit Quest Completer - CDP Patch Script
        // Drop an updated copy into <AppFolder>\Scripts\cdp_patch.js or import via the app.
        globalThis.__DQC_ADAPTER = {
            version: "1.0.0",

            getMods() {
                if (window.Vencord?.Webpack) {
                    const W = window.Vencord.Webpack;
                    return {
                        QuestStore: W.findStore('QuestStore') || W.findStore('QuestsStore'),
                        OrbStore: W.findStore('VirtualCurrencyStore'),
                        API: W.Common?.RestAPI || W.findByProps('get', 'post', 'del')
                    };
                }

                let req;
                webpackChunkdiscord_app.push([[Symbol()], {}, r => {
                    if (Object.keys(r?.c || {}).length > Object.keys(req?.c || {}).length) req = r;
                }]);
                webpackChunkdiscord_app.pop();

                const mods = Object.values(req?.c || {});
                const findStore = name => {
                    for (const m of mods) {
                        const exp = m?.exports;
                        if (!exp || typeof exp !== 'object') continue;
                        for (const k of Object.keys(exp)) {
                            const p = exp[k];
                            if (p && typeof p === 'object' && p.__proto__?.constructor?.displayName === name) return p;
                        }
                    }
                };
                const findApi = () => {
                    for (const m of mods) {
                        const exp = m?.exports;
                        if (!exp || typeof exp !== 'object') continue;
                        for (const k of Object.keys(exp)) {
                            const p = exp[k];
                            if (p && typeof p.get === 'function' && typeof p.post === 'function' && typeof p.del === 'function' && !p._dispatcher) return p;
                        }
                    }
                };

                return {
                    QuestStore: findStore('QuestStore'),
                    OrbStore: findStore('VirtualCurrencyStore'),
                    API: findApi()
                };
            },

            async fetchSnapshot() {
                const { QuestStore, OrbStore, API } = this.getMods();
                if (!QuestStore) {
                    throw new Error('QuestStore not found. On Discord Stable, Vencord or Canary/PTB is needed for webpack access.');
                }

                const raw = QuestStore.quests;
                const list = raw instanceof Map ? [...raw.values()] : Object.values(raw || {});
                const taskKeys = t => t instanceof Map ? [...t.keys()] : Object.keys(t || {});
                const taskAt = (t, k) => t instanceof Map ? t.get(k) : t?.[k];
                const now = Date.now();
                const out = [];

                for (const q of list) {
                    if (q.userStatus?.completedAt) continue;
                    const exp = new Date(q.config?.expiresAt ?? NaN).getTime();
                    if (!Number.isNaN(exp) && exp <= now) continue;

                    const cfg = taskKeys(q.config?.taskConfigV2?.tasks).length > 0
                        ? q.config.taskConfigV2
                        : q.config?.taskConfig;
                    const keys = taskKeys(cfg?.tasks);
                    if (!keys.length) continue;

                    let taskType = 'UNSUPPORTED', taskKey = keys[0];
                    if (keys.includes('PLAY_ON_DESKTOP')) {
                        taskType = 'GAME'; taskKey = 'PLAY_ON_DESKTOP';
                    } else if (keys.includes('WATCH_VIDEO')) {
                        taskType = 'VIDEO'; taskKey = 'WATCH_VIDEO';
                    } else if (keys.some(k => k.includes('VIDEO'))) {
                        taskType = 'VIDEO'; taskKey = keys.find(k => k.includes('VIDEO'));
                    } else if (keys.some(k => k.startsWith('PLAY') && k !== 'PLAY_ON_XBOX' && k !== 'PLAY_ON_PLAYSTATION' && k !== 'PLAY_ACTIVITY')) {
                        taskType = 'GAME';
                        taskKey = keys.find(k => k.startsWith('PLAY') && k !== 'PLAY_ON_XBOX' && k !== 'PLAY_ON_PLAYSTATION' && k !== 'PLAY_ACTIVITY');
                    }

                    const taskObj = taskAt(cfg.tasks, taskKey);
                    const appId = taskObj?.applications?.[0]?.id ?? q.config?.application?.id ?? null;
                    const progMap = q.userStatus?.progress;
                    const progEntry = progMap instanceof Map ? progMap.get(taskKey) : progMap?.[taskKey];
                    const rewards = q.config?.rewardsConfig?.rewards || [];
                    const orbs = rewards.reduce((a, r) => a + (Number(r?.orbQuantity) || 0), 0);
                    const rewardLabel = orbs > 0 ? `${orbs} Orbs` : (rewards[0]?.messages?.name ?? 'Other');

                    out.push({
                        id: q.id,
                        name: q.config?.messages?.questName ?? q.id,
                        taskType,
                        taskKey,
                        appId: appId ? String(appId) : null,
                        target: Number(taskObj?.target ?? 0),
                        progress: Number(progEntry?.value ?? 0),
                        enrolled: Boolean(q.userStatus?.enrolledAt),
                        orbs,
                        rewardLabel,
                        sealed: q.trafficMetadataSealed ?? null
                    });
                }

                let orbBal = OrbStore?.getCurrentBalance?.() ?? OrbStore?.balance ?? null;
                if (typeof orbBal !== 'number' || !Number.isFinite(orbBal)) {
                    try {
                        const balRes = await API?.get?.({ url: '/users/@me/virtual-currency/balance' });
                        const fetched = balRes?.body?.balance;
                        if (typeof fetched === 'number' && Number.isFinite(fetched)) orbBal = fetched;
                    } catch { }
                }

                return {
                    suspendedUntil: QuestStore.questAccessSuspendedUntil ?? null,
                    enrollmentBlockedUntil: QuestStore.questEnrollmentBlockedUntil ?? null,
                    orbBalance: typeof orbBal === 'number' ? orbBal : null,
                    quests: out
                };
            },

            async enrollQuest(questId, trafficMetadataSealed) {
                const { API } = this.getMods();
                await API.post({
                    url: `/quests/${questId}/enroll`,
                    body: {
                        location: 11,
                        is_targeted: false,
                        metadata_sealed: null,
                        traffic_metadata_sealed: trafficMetadataSealed ?? null
                    }
                });
                return "ok";
            },

            async sendVideoProgress(questId, taskKey, timestamp) {
                const { API } = this.getMods();
                const res = await API.post({
                    url: `/quests/${questId}/video-progress`,
                    body: { timestamp }
                });
                const b = res?.body || {};
                const v = b.progress?.[taskKey]?.value ?? b.progress?.WATCH_VIDEO?.value ?? null;
                return { completed: Boolean(b.completed_at), serverVal: v };
            }
        };
        """;
}