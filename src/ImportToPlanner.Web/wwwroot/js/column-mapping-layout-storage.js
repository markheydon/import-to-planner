window.importToPlannerColumnMappingStorage = {
    getLayout: function (storeKey, layoutSignature) {
        var raw = window.localStorage.getItem(storeKey);
        if (!raw) {
            return null;
        }

        try {
            var store = JSON.parse(raw);
            return store[layoutSignature] ?? null;
        } catch {
            return null;
        }
    },
    saveLayout: function (storeKey, layoutSignature, payload) {
        var raw = window.localStorage.getItem(storeKey);
        var store = {};
        if (raw) {
            try {
                store = JSON.parse(raw) ?? {};
            } catch {
                store = {};
            }
        }

        store[layoutSignature] = payload;
        window.localStorage.setItem(storeKey, JSON.stringify(store));
    }
};
