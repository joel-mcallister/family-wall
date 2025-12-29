function loadCalendar() {
    var calendarEl = document.getElementById('calendar');

    var calendar = new FullCalendar.Calendar(calendarEl, {
        themeSystem: 'bootstrap5',
        initialView: 'timeGridWeek',
        slotMinTime: '06:30:00',
        firstDay: (new Date().getDay()),
        headerToolbar: {
            left: 'prev,next today newEvent',
            center: 'title',
            right: 'dayGridMonth,timeGridWeek,timeGridDay,listWeek'
        },
        customButtons: {
            newEvent: {
                text: '',
                icon: 'plus-square',
                hint: 'Add Calendar Item',
                click: function () {
                    const modalEl = document.getElementById('eventMaintModal');
                    if (!modalEl || typeof bootstrap === 'undefined') {
                        // Fallback: try to trigger the existing button in the page if present
                        const pageButton = document.querySelector('button[data-bs-target="#eventMaintModal"]');
                        if (pageButton) pageButton.click();
                        return;
                    }
                    bootstrap.Modal.getOrCreateInstance(modalEl).show();
                }
            }
        },
        height: 'auto',
        navLinks: true,
        editable: false,
        dayMaxEvents: true,
        events: function (info, successCallback, failureCallback) {
            fetch('/?handler=Events&start=' + info.startStr + '&end=' + info.endStr)
                .then(response => response.json())
                .then(data => {
                    successCallback(data);
                })
                .catch(error => {
                    console.error('Error loading events:', error);
                    failureCallback(error);
                });
        },
        eventClick: function (info) {
            showEventDetails(info.event);
        },
        eventDidMount: function (info) {
            // Add tooltip
            if (info.event.extendedProps.description) {
                info.el.title = info.event.extendedProps.description;
            }
        }
    });

    calendar.render();

    let now = new Date();
    let duration = {
        hours: now.getHours(),
        minutes: now.getMinutes(),
        seconds: now.getSeconds()
    };

    calendar.scrollToTime(duration);

    // Event details modal
    function showEventDetails(event) {
        var $modal = $('#eventModal');
        var $title = $('#eventTitle');
        var $details = $('#eventDetails');
        
        $('#calDel').attr('href', '/?handler=CalendarDelete&id=' + event.id);
        
        $title.text(event && event.title ? event.title : '');

        var $container = $('<div />');
        var ext = (event && event.extendedProps) ? event.extendedProps : {};

        if (event && event.allDay) {
            // <p><strong>When:</strong> All Day</p>
            $container.append(
                $('<p />').append(
                    $('<strong />').text('When:'),
                    ' ',
                    'All Day'
                )
            );
        } else {
            var startDate = event && event.start ? event.start.toLocaleString() : '';
            var endDate = event && event.end ? event.end.toLocaleString() : '';

            // <p><strong>Start:</strong> startDate</p>
            $container.append(
                $('<p />').append(
                    $('<strong />').text('Start:'),
                    ' ',
                    startDate
                )
            );

            if (endDate) {
                // <p><strong>End:</strong> endDate</p>
                $container.append(
                    $('<p />').append(
                        $('<strong />').text('End:'),
                        ' ',
                        endDate
                    )
                );
            }
        }

        if (ext.description) {
            // <p><strong>Description:</strong></p>
            // <p>description text</p>
            $container.append(
                $('<p />').append($('<strong />').text('Description:'))
            );
            $container.append(
                $('<p />').text(ext.description)
            );
        }

        if (ext.location) {
            // <p><strong>Location:</strong> location</p>
            $container.append(
                $('<p />').append(
                    $('<strong />').text('Location:'),
                    ' ',
                    ext.location
                )
            );
        }

        // Replace innerHTML with jQuery-safe append
        if ($details && $details.length) {
            $details.empty().append($container.children());
        } else {
            console.warn('Details element #eventDetails not found.');
        }

        if ($modal && $modal.length) {
            try {
                if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
                    var bsModal = bootstrap.Modal.getOrCreateInstance($modal[0]);
                    bsModal.show();
                } else if (typeof jQuery !== 'undefined' && typeof $modal.modal === 'function') {
                    // Fallback for Bootstrap 4 with jQuery
                    $modal.modal('show');
                } else {
                    console.warn('Bootstrap modal API not available. Ensure Bootstrap JS is loaded.');
                }
            } catch (err) {
                console.error('Error showing modal:', err);
            }
        } else {
            console.warn('Modal element #eventModal not found.');
        }
    }



    /*
    // Close modal
    var span = document.getElementsByClassName('close')[0];
    span.onclick = function () {
        document.getElementById('eventModal').style.display = 'none';
    }

    window.onclick = function (event) {
        var modal = document.getElementById('eventModal');
        if (event.target == modal) {
            modal.style.display = 'none';
        }
    }
    */

}

// Client-side validation to ensure EndDate is not before StartDate.
(function () {
    'use strict';

    function toDate(value) {
        // For datetime-local values like "2025-12-19T14:30"
        // the Date constructor treats this as local time.
        return value ? new Date(value) : null;
    }

    function setupValidation(form) {
        if (!form) return;

        var startEl = form.querySelector('#StartDate');
        var endEl = form.querySelector('#EndDate');

        if (!startEl || !endEl) return;

        // When start changes, set min on end to prevent picking earlier values in browsers that support it.
        function onStartChange() {
            try {
                if (startEl.value) {
                    endEl.min = startEl.value;
                } else {
                    endEl.removeAttribute('min');
                }

                // If end exists and is earlier than start, adjust end to start to keep a valid state.
                if (endEl.value && startEl.value) {
                    var s = toDate(startEl.value);
                    var e = toDate(endEl.value);
                    if (e < s) {
                        endEl.value = startEl.value;
                    }
                }
            } catch (ex) {
                // noop
            }
        }

        startEl.addEventListener('input', onStartChange);
        startEl.addEventListener('change', onStartChange);

        form.addEventListener('submit', function (evt) {
            // If either field is empty, allow submission (server-side should validate if required).
            if (!startEl.value || !endEl.value) {
                return;
            }

            var start = toDate(startEl.value);
            var end = toDate(endEl.value);

            if (start && end && end < start) {
                evt.preventDefault();
                evt.stopPropagation();

                // Provide inline visual feedback if Bootstrap form classes are present.
                // Add is-invalid to end input and focus it.
                endEl.classList.add('is-invalid');

                // Create or update an inline feedback element next to the input.
                var feedbackId = 'endDateFeedback';
                var feedback = form.querySelector('#' + feedbackId);
                if (!feedback) {
                    feedback = document.createElement('div');
                    feedback.id = feedbackId;
                    feedback.className = 'invalid-feedback';
                    endEl.parentNode.appendChild(feedback);
                }
                feedback.textContent = 'End date/time cannot be before start date/time.';
                endEl.focus();
            } else {
                // remove any previous invalid state
                endEl.classList.remove('is-invalid');
                var old = form.querySelector('#endDateFeedback');
                if (old) old.textContent = '';
            }
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        // Setup validation for the Add/Edit modal form and the Update form if present.
        var addForm = document.querySelector('#eventMaintModal form'); // CalendarAdd
        var updateForm = document.querySelector('#eventModal form'); // CalendarUpdate

        setupValidation(addForm);
        setupValidation(updateForm);
    });
})();

$(function () {
    loadCalendar();
    setInterval(loadCalendar, 30 * 60 * 1000); // Refresh forecast every 30 minutes
});

