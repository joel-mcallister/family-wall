function safeCssUrl(url) {
    return "url('" + url.replace(/'/g, "\\'") + "')";
}

function applyBackground(url) {
    $('#backgroundLayer').css('background-image', safeCssUrl(url));
}

function swapWithFade(url) {
    var $overlay = $('#blackOverlay');
    var $img = $('#randomImage');

    // Preload handled by caller; this function handles the transition and swap.
    // Start fade to black and fade out image.
    $overlay.addClass('visible');
    $img.addClass('fading');

    // Wait for overlay transition to finish (fade to black complete), then swap.
    $overlay.one('transitionend', function (e) {
        // Ensure the transitionend was for opacity (some browsers emit multiple)
        if (e.originalEvent && e.originalEvent.propertyName && e.originalEvent.propertyName !== 'opacity') {
            return;
        }

        // Update background and foreground src
        applyBackground(url);
        $img.attr('src', url);

        // Small delay to ensure DOM updated then fade back in
        setTimeout(function () {
            $overlay.removeClass('visible');
            $img.removeClass('fading');
        }, 40);
    });
}

function loadRandomImage() {
    fetch('./?handler=RandomImage', { cache: 'no-store' })
        .then(function (response) {
            if (!response.ok) throw new Error('Network response was not ok');
            return response.json();
        })
        .then(function (data) {
            if (!data || !data.url) throw new Error('Invalid image data');

            var url = data.url;

            // Preload the image first, then perform the fade-swap so black covers smoothly.
            var temp = new Image();
            temp.onload = function () {
                swapWithFade(url);
            };
            temp.onerror = function () {
                console.error('Failed to preload image:', url);
                // Still attempt the swap to surface the failure (browser may show broken image)
                swapWithFade(url);
            };
            temp.src = url;
        })
        .catch(function (err) {
            console.error('Failed to load image:', err);
        });
}

$(function () {
    // Initial load: fetch and set background & foreground without visible flicker.
    fetch('./?handler=RandomImage', { cache: 'no-store' })
        .then(function (r) { if (!r.ok) throw new Error('Network'); return r.json(); })
        .then(function (d) {
            if (d && d.url) {
                // Preload then set immediately without overlay animation on first load
                var temp = new Image();
                temp.onload = function () {
                    applyBackground(d.url);
                    $('#randomImage').attr('src', d.url);
                };
                temp.onerror = function () {
                    applyBackground(d.url);
                    $('#randomImage').attr('src', d.url);
                };
                temp.src = d.url;
            }
        })
        .catch(function (e) { console.error(e); })
        .finally(function () {
            // Regular refresh cycle and click-to-refresh
            setInterval(loadRandomImage, 60 * 1000);
            $('#randomImageBody').on('click', function () {
                loadRandomImage();
            });
            // Also allow clicking the image itself
            $('#randomImage').on('click', function (ev) {
                ev.stopPropagation();
                loadRandomImage();
            });
        });
});