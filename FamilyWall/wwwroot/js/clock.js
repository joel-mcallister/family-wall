(function () {
    var el = document.getElementById('liveDateTime');
    if (!el) return;

    var formatter = new Intl.DateTimeFormat(undefined, {
        weekday: 'long',
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: 'numeric',
        minute: '2-digit'
    });

    function update() {
        el.textContent = formatter.format(new Date());
    }

    // Initial update
    update();

    // Schedule first tick at the start of the next minute, then every minute.
    var now = new Date();
    var msToNextMinute = (60 - now.getSeconds()) * 1000 - now.getMilliseconds();
    setTimeout(function () {
        update();
        setInterval(update, 60 * 1000);
    }, msToNextMinute);
})();