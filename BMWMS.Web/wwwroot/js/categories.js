$(document).ready(function () {
    loadCategories();
});

function loadCategories() {
    $('#loading').show();
    $('#categoriesTable').hide();
    $('#errorMessage').hide();

    ApiClient.get('categories', 
        function(data) {
            displayCategories(data);
        },
        function(error) {
            showError('Error loading categories: ' + error);
        }
    );
}

function displayCategories(data) {
    $('#loading').hide();
    
    if (data && data.length > 0) {
        let html = '';
        $.each(data, function (index, category) {
            html += `<tr>
                <td>${category.categoryId}</td>
                <td>${category.categoryName}</td>
                <td>${category.description || ''}</td>
            </tr>`;
        });
        
        $('#categoriesBody').html(html);
        $('#categoriesTable').show();
    } else {
        showError('No categories found.');
    }
}

function showError(message) {
    $('#loading').hide();
    $('#errorMessage').text(message).show();
    console.error(message);
}$(document).ready(function () {
    loadCategories();
});

