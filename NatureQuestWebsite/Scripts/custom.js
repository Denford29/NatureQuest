(function ($) {
    "use strict";

    //set the functions included
    var siteScripts = {
        onReady: function () {
            this.productSortOptions();
        },

        //create the function to do the sorting
        productSortOptions: function () {
            //set the event to listen to the change event
            $("#SortOption").change(function () {
                //get the other
                var currentPage = $("#listPaging_CurrentPage").val();
                var pageUrl = $("#currentPage_Url").val();
                //get selected option
                var sortOption = $('#SortOption :selected').val();
                //if we have the selected option then re submit the page
                if (sortOption !== "" && pageUrl !== "" && currentPage !== "") {
                    var submitUrl = pageUrl + "?page=" + currentPage + "&sortOption=" + sortOption;
                    //redirect to the sorted page
                    window.location.replace(submitUrl);
                }
            });
        }
    };

    // on doc ready load the defined main function
    $(document).ready(function () {
        siteScripts.onReady();
    });

})(jQuery);