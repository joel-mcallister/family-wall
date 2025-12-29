

function safeCssUrl(url) {
    return "url('" + url.replace(/'/g, "\\'") + "')";
}

function applyBackground(url) {
    $('body').css('background-image', 'none');
    $('#backgroundLayer').css('background-image', safeCssUrl(url));
}

window.imageId = '';
window.file = '';
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
            window.imageId = data.id;
            window.file = data.url;

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

            // Remove all child items from cornerMessage
            var corner = document.getElementById('cornerMessage');
            if (corner) {
                while (corner.firstChild) {
                    corner.removeChild(corner.firstChild);
                }
            }
            
            if (data.taken) {
                var dateTaken = $('<div />').text(data.taken);
                $('#cornerMessage').show().append(dateTaken);
            }
            else {
                $('#cornerMessage').hide();
            }
        })
        .catch(function (err) {
            console.error('Failed to load image:', err);
        });
}

$(function () {
    $('#liveToast').hide();

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

document.addEventListener("DOMContentLoaded", function () {
    const rotateBtn = document.getElementById("rotateBtn");
    const deleteBtn = document.getElementById("deleteBtn");
    
    rotateBtn.addEventListener("click", function () {
        $('#toast-message').text('Roatating image...');
        $('#liveToast').show();

        fetch("/Slideshow?handler=Rotate&dir=right&file=" + $('#randomImage').attr('src'), {
            method: "GET"
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error("Network response was not ok");
                }
               
            })
            .then(data => {
                $('#toast-message').text('Image rotated!');
                applyBackground($('#randomImage').attr('src'));
                $('#randomImage').attr('src', $('#randomImage').attr('src'));

                setTimeout(() => {
                    $('#liveToast').hide();
                }, 5000);
            })
            .catch((error) => {
                $('#toast-message').text('Error');
                
                setTimeout(() => {
                    $('#liveToast').hide();
                }, 5000);
                console.error("There was a problem with the fetch operation:", error);
            });
    });

    deleteBtn.addEventListener("click", function () {
        if (confirm("Are you sure you want to delete this image?")) {
            $('#toast-message').text('Deleting image...');
            $('#liveToast').show();

            fetch("/Slideshow?handler=Delete&file=" + $('#randomImage').attr('src'), {
                method: "GET"
            })
                .then(response => {
                    if (!response.ok) {
                        throw new Error("Network response was not ok");
                    }
                })
                .then(data => {
                    $('#toast-message').text('Deleted');

                    setTimeout(() => {
                        $('#liveToast').hide();
                    }, 5000);
                })
                .catch((error) => {
                    $('#toast-message').text('Error Deleting');

                    setTimeout(() => {
                        $('#liveToast').hide();
                    }, 5000);
                    console.error("There was a problem with the fetch operation:", error);
                });
        }
    });
});