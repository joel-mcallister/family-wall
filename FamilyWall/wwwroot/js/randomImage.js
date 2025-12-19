function loadRandomImage() {
    fetch('./?handler=RandomImage', { cache: 'no-store' })
        .then(response => {
            if (!response.ok) throw new Error('Network response was not ok');
            return response.json();
        })
        .then(data => {

            $('#randomImage').addClass('transitioning-src'); // Add class to begin transition
            setTimeout(() => {
                $('#randomImage').attr('src', data.url).removeClass('transitioning-src');
            }, 1000);

        })
        .catch(err => {
            console.error('Failed to load image:', err);
            /*
            if (forecastContainer) {
                forecastContainer.innerHTML = '<div class="col-12"><div class="card mb-30"><div class="card-body">Unable to load forecast.</div></div></div>';
            }
            */
        });
}

$(function () {
    loadRandomImage();
    setInterval(loadRandomImage, 60 * 1000); // Refresh random image every 1 minute
});
