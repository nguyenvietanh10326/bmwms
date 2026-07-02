const ApiClient = {
    get: function(endpoint, successCallback, errorCallback) {
        const url = getApiUrl(endpoint);
        
        $.ajax({
            url: url,
            type: 'GET',
            dataType: 'json',
            success: function(data) {
                if (successCallback) {
                    successCallback(data);
                }
            },
            error: function(xhr, status, error) {
                if (errorCallback) {
                    errorCallback(error, xhr);
                } else {
                    console.error('API Error:', error);
                }
            }
        });
    },

    post: function(endpoint, data, successCallback, errorCallback) {
        const url = getApiUrl(endpoint);
        
        $.ajax({
            url: url,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data),
            dataType: 'json',
            success: function(data) {
                if (successCallback) {
                    successCallback(data);
                }
            },
            error: function(xhr, status, error) {
                if (errorCallback) {
                    errorCallback(error, xhr);
                } else {
                    console.error('API Error:', error);
                }
            }
        });
    }
}
