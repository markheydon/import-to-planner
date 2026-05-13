window.importToPlannerThemeStorage = {
    getThemeMode: function (key) {
        return window.sessionStorage.getItem(key);
    },
    setThemeMode: function (key, mode) {
        window.sessionStorage.setItem(key, mode);
    }
};
