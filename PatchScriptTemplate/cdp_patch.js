// OrbKit Quest Completer - CDP Patch Script Template
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