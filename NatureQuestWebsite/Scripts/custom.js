(function ($) {
    "use strict";

    //set the functions included
    var siteScripts = {
        onReady: function () {
            this.productSortOptions();
            this.addProductToCart();
            this.clearCart();
            this.removeCartItem();
            this.updateCartItem();
            this.shippingOptions();
        },

        //create the function to do the sorting
        productSortOptions: function () {
            //set the event to listen to the change event
            $("#SortOption").change(function () {
                //get the other
                var currentPage = $("#listPaging_CurrentPage").val();
                var pageUrl = $("#currentPage_Url").val();
                //get selected option
                var sortOption = $("#SortOption :selected").val();
                //if we have the selected option then re submit the page
                if (sortOption !== "" && pageUrl !== "" && currentPage !== "") {
                    var submitUrl = pageUrl + "?page=" + currentPage + "&sortOption=" + sortOption;
                    //redirect to the sorted page
                    window.location.replace(submitUrl);
                }
            });
        },

        //create the function to add the product to the cart
        addProductToCart: function () {

            $("#AddToCartBtn").click(function (event) {
                event.preventDefault();
                //load the spinner
                $.busyLoadFull("show", {
                    background: "rgba(0, 0, 0, 0.21)",
                    spinner: "circles",
                    animation: "slide",
                    text: "ADDING PRODUCT ...",
                    textPosition: "bottom"
                });
                //get the form to submit
                var productForm = $(this).parents('form:first');
                if (productForm !== undefined && productForm !== null) {
                    productForm.submit();
                }
            });
        },

        //create the function to clear the cart
        clearCart: function () {

            $("#ClearChoppingCart").click(function (event) {
                event.preventDefault();
                //load the spinner
                $.busyLoadFull("show", {
                    background: "rgba(0, 0, 0, 0.21)",
                    spinner: "circles",
                    animation: "slide",
                    text: "CLEARING CART ...",
                    textPosition: "bottom"
                });
                //get the form to submit
                var clearCartForm = $(this).parents('form:first');
                if (clearCartForm !== undefined && clearCartForm !== null) {
                    clearCartForm.submit();
                }
            });
        },

        //create the function to remove the cart item
        removeCartItem: function () {

            $(".removeCartProduct").click(function (event) {
                event.preventDefault();
                //load the spinner
                $.busyLoadFull("show", {
                    background: "rgba(0, 0, 0, 0.21)",
                    spinner: "circles",
                    animation: "slide",
                    text: "REMOVING ITEM ...",
                    textPosition: "bottom"
                });
                //get the form to submit
                var removeCartItemForm = $(this).parents('form:first');
                if (removeCartItemForm !== undefined && removeCartItemForm !== null) {
                    removeCartItemForm.submit();
                }
            });
        },

        //create the function to clear the cart
        updateCartItem: function () {

            $(".updateCartBtn").click(function (event) {
                event.preventDefault();
                //load the spinner
                $.busyLoadFull("show", {
                    background: "rgba(0, 0, 0, 0.21)",
                    spinner: "circles",
                    animation: "slide",
                    text: "UPDATING ITEM ...",
                    textPosition: "bottom"
                });

                //get the form to submit
                var removeCartItemForm = $(this).parents('form:first');
                if (removeCartItemForm !== undefined && removeCartItemForm !== null) {
                    removeCartItemForm.submit();
                }
            });
        },

        //create the function to do the sorting
        shippingOptions: function () {
            //set the event to listen to the change event
            $('input:radio[name="ShippingOption"]').change(function () {
                //load the spinner
                $.busyLoadFull("show", {
                    background: "rgba(0, 0, 0, 0.21)",
                    spinner: "circles",
                    animation: "slide",
                    text: "UPDATING SHIPPING ...",
                    textPosition: "bottom"
                });
                //get selected option
                var shippingOption = $('input[name="ShippingOption"]:checked').val();
                //if we have the selected option then update the selected option and submit the form
                if (shippingOption !== "") {
                    var updateShippingForm = $(this).parents('form:first');
                    if (updateShippingForm !== undefined && updateShippingForm !== null) {
                        updateShippingForm.submit();
                    }
                }
            });
        }

    };

    // on doc ready load the defined main function
    $(document).ready(function () {
        siteScripts.onReady();
    });

})(jQuery);


            //$("#AddToCartBtn").click(function () {
            //    //get selected price page id
            //    var selectedPricePageId = $("#SelectedPricePageId :selected").val();
            //    //get set quantity
            //    var selectedQuantity = $("#SelectedQuantity").val();
            //    //check the values
            //    if (selectedPricePageId !== "" && selectedQuantity !== "") {
            //        //create the model to send
            //        var productModel = {
            //            SelectedPricePageId: selectedPricePageId,
            //            SelectedQuantity: parseInt(selectedQuantity)
            //        };

            //        //create the ajax call
            //        $.ajax({
            //            url: "/Umbraco/Api/Shop/AddProductToCart",
            //            type: "POST",
            //            cache: false,
            //            async: false,
            //            data: JSON.stringify(productModel),
            //            dataType: "json",
            //            contentType: "application/json; charset=utf-8",
            //            success: function(msg) {
            //                //check if we get a success or an error back
            //                if (msg.ProductAdded) {
            //                    iziToast.success({
            //                        title: "Success",
            //                        message: msg.ResultMessage,
            //                        timeout: 10000,
            //                        position: "topRight"
            //                    });
            //                } else {
            //                    iziToast.error({
            //                        title: "Error",
            //                        message: msg.ResultMessage,
            //                        timeout: 10000,
            //                        position: "topRight"
            //                    });
            //                }
            //            },
            //            error: function(xhr, ajaxOptions, thrownError) {

            //                iziToast.error({
            //                    title: "Error",
            //                    message: xhr.message,
            //                    timeout: 10000,
            //                    position: "topRight"
            //                });
            //            }
            //        });
            //    }
            //    //show the default cart adding error message
            //    else {
            //        iziToast.error({
            //            title: "Error",
            //            message: "Please select the size and enter your quantity",
            //            timeout: 10000,
            //            position: "topRight"
            //        });
            //    }
            //});
