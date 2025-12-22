function formatCategories(cat) {
    if (!cat) {
        return '';
    }
    if (Array.isArray(cat)) {
        var cats = cat.join(', ');

        if (cats == 'Blue category') {
            return $('<span />').addClass('badge').addClass('bg-primary').append($('<i />').addClass('fa-solid fa-circle'));
        }

        return cats;
    }
    return String(cat);
}

function clearTodolist(tbody) {
    while (tbody.firstChild) {
        tbody.removeChild(tbody.firstChild);
    }
}

async function loadToDoList() {
    var tbody = document.getElementById('todolist');
    if (!tbody) {
        console.warn('Element with id "todolist" not found.');
        return;
    }

    try {
        $.ajax({
            url: './?handler=ToDo',
            method: 'GET',
            dataType: 'json',
            success: function (data) {
                try {
                    if (!Array.isArray(data)) {
                        console.warn('ToDo handler returned non-array data:', data);
                        clearTodolist(tbody);
                        return;
                    }

                    clearTodolist(tbody);

                    // Pseudocode:
                    // - For each item in data:
                    //   - compute id, title, created, categories, due
                    //   - create jQuery elements for <tr> and <td>s
                    //   - set attributes and text on those elements
                    //   - append categories (supports string or jQuery element returned by formatCategories)
                    //   - append the tds to the tr in the correct order
                    //   - append the tr to the existing tbody (convert DOM tbody to jQuery with $(tbody))
                    // Implementation (jQuery-style element creation and append):

                    data.forEach(function (item) {
                        var id = item.id != null ? item.id : '';
                        var title = item.title != null ? item.title : '';
                        var created = formatDateTime(item.createdDateTime);
                        var categories = formatCategories(item.categories);
                        var due = formatDateTime(item.dueDate);
                        var icon = item.icon;

                        var $tr = $('<tr />').attr('data-id', String(id));

                        var $tdImage = $('<td />').append($('<i />').addClass(icon));

                        var $tdTitle = $('<td />')
                            .text(title)
                            .attr('data-id', String(id));

                        var $tdCreated = $('<td />').text(created);

                        var $tdCategories = $('<td />');
                        $tdCategories.append(categories);

                        var $tdDue = $('<td />').text(due);

                        /*
                        <div class="action justify-content-end">
                        <button class="more-btn ml-10 dropdown-toggle" id="moreAction1" data-bs-toggle="dropdown"
                                aria-expanded="false">
                            <i class="lni lni-more-alt"></i>
                        </button>
                        <ul class="dropdown-menu dropdown-menu-end" aria-labelledby="moreAction1">
                            <li class="dropdown-item">
                                <a href="#0" class="text-gray">Remove</a>
                            </li>
                            <li class="dropdown-item">
                                <a href="#0" class="text-gray">Edit</a>
                            </li>
                        </ul>
                    </div>
                    */

                        // Create action dropdown as jQuery elements so it can be appended to the table row
                        var actionId = 'moreAction' + (id ? String(id) : String(Math.floor(Math.random() * 1000000)));

                        var $actionButton = $('<button />')
                            .addClass('more-btn ml-10 dropdown-toggle')
                            .attr({
                                id: actionId,
                                'data-bs-toggle': 'dropdown',
                                'aria-expanded': 'false',
                                type: 'button'
                            })
                            .append($('<i />').addClass('lni lni-more-alt'));

                        var $actionMenu = $('<ul />')
                            .addClass('dropdown-menu dropdown-menu-end')
                            .attr('aria-labelledby', actionId)
                            .append(
                                $('<li />').addClass('dropdown-item').append(
                                    $('<a />').attr('href', '#0').addClass('text-gray').text('Remove')
                                ),
                                $('<li />').addClass('dropdown-item').append(
                                    $('<a />').attr('href', '#0').addClass('text-gray').text('Edit')
                                )
                            );

                        var $actionDiv = $('<div />')
                            .addClass('action justify-content-end')
                            .append($actionButton, $actionMenu);

                        var $tdActions = $('<td />')
                            .addClass('text-end')
                            .append($actionDiv);

                        // Append the actions cell to the row (place at the end)
                        $tr.append($tdImage, $tdTitle, $tdCategories, $tdDue, $tdCreated, $tdActions);

                        $(tbody).append($tr);
                    });
                } catch (innerErr) {
                    console.error('Error processing ToDo data:', innerErr);
                    clearTodolist(tbody);
                }
            },
            error: function (jqXHR, textStatus, errorThrown) {
                console.error('Failed to load ToDo items. HTTP status:', jqXHR.status, textStatus, errorThrown);
                clearTodolist(tbody);
            }
        });
    } catch (err) {
        console.error('Error loading ToDo list:', err);
    }
}

$(function () {
    loadToDoList();
    setInterval(loadToDoList, 20 * 60 * 1000); // Refresh todo every 20 minutes
});
   
