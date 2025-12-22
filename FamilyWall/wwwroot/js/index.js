// Redirect to /Sync if the client's local hour is 3 (3 AM).
// Guard: don't redirect if already on /Sync to avoid loop.
(function () {
    try {
        var now = new Date();
        // now.getHours() returns 0-23 local hour
        if (now.getHours() === 3 && window.location.pathname !== '/Sync') {
            // Use replace() to avoid adding to history (optional)
            window.location.replace('/Sync');
        }
    } catch (e) {
        console && console.error && console.error('Redirect check failed', e);
    }
})();