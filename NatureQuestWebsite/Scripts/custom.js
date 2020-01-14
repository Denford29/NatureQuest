(function ($) {
    "use strict";

    //set the functions included
    var siteScripts = {
        onReady: function () {
            this.productSortOptions();
            this.addProductToCart();
            this.updateDisplayedProductDetails();
            this.clearCart();
            this.submitCart();
            this.removeCartItem();
            this.updateCartItem();
            this.shippingOptions();
            this.cartReview();
            this.submitLoginForm();
            this.submitRegistrationForm();
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

            $(".AddToCartBtn").click(function (event) {
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

        //create the function to update displayed product details
        updateDisplayedProductDetails: function() {

            $('#SelectedPricePageId').change(function() {
                //load the spinner
                $.busyLoadFull("show",
                    {
                        background: "rgba(0, 0, 0, 0.21)",
                        spinner: "circles",
                        animation: "slide",
                        text: "UPDATING SELECTED PRICE ...",
                        textPosition: "bottom"
                    });
                //get selected option
                var selectedPriceOption = $('select#SelectedPricePageId option:checked').val();
                if (selectedPriceOption !== undefined && selectedPriceOption !== null) {
                    $("#selectedFeaturePriceId").val(selectedPriceOption);
                    //get the form and submit it
                    $("#updateFeaturePrice").submit();
                }
                //hide the loading
                $.busyLoadFull("hide");
            });
        },

        //create the function to clear the cart
        clearCart: function () {

            $(".ClearChoppingCart").click(function (event) {
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

        //submit the cart
        submitCart: function() {
            $("#checkoutSubmitButton").click(function() {
                //load the spinner
                $.busyLoadFull("show",
                    {
                        background: "rgba(0, 0, 0, 0.21)",
                        spinner: "circles",
                        animation: "slide",
                        text: "FINALISING YOUR CART ...",
                        textPosition: "bottom"
                    });
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
        },

        cartReview: function() {
            $(".cart-review").on('click', function (event) {
                event.stopPropagation();
                event.stopImmediatePropagation();
                //load the spinner
                $.busyLoadFull("show", {
                    background: "rgba(0, 0, 0, 0.21)",
                    spinner: "circles",
                    animation: "slide",
                    text: "UPDATING SHIPPING ...",
                    textPosition: "bottom"
                });

                //check if we have shipping selected
                var shippingOption = $('input[name="ShippingOption"]:checked').val();
                //if we don''t have shipping selected, show an error message
                if (shippingOption === "") {
                    $.busyLoadFull("hide");
                    iziToast.error({
                        title: "Error",
                        message: "Please select your shipping method",
                        timeout: 10000,
                        position: "topRight"
                    });
                }

                //check the shipping details
                var shippingFullname = $("#cartMember_FullName").val();
                var shippingEmail = $("#cartMember_Email").val();
                var shippingAddress = $("#cartMember_HouseAddress").val();
                var shippingMobileNumber = $("#cartMember_MobileNumber").val();
                //if we don''t have shipping selected, show an error message
                if (shippingFullname === "" ||
                    shippingEmail === "" ||
                    shippingAddress === "" ||
                    shippingMobileNumber === "") {
                    $.busyLoadFull("hide");
                    iziToast.error({
                        title: "Error",
                        message: "Please enter your shipping details",
                        timeout: 10000,
                        position: "topRight"
                    });
                } else {
                    //add the values to the form value
                    $("#shippingFullname").val(shippingFullname);
                    $("#shippingEmail").val(shippingEmail);
                    $("#shippingAddress").val(shippingAddress);
                    $("#shippingMobileNumber").val(shippingMobileNumber);
                    //get the form and submit it
                    $("#shippingDetailsForm").submit();
                    //$.busyLoadFull("hide");
                }
            });
        },

        //create the function to submit the login form
        submitLoginForm: function () {

            $("#submitLoginForm").click(function (event) {
                event.preventDefault();
                //load the spinner
                $.busyLoadFull("show", {
                    background: "rgba(0, 0, 0, 0.21)",
                    spinner: "circles",
                    animation: "slide",
                    text: "LOGGING YOU IN ...",
                    textPosition: "bottom"
                });
                //get the form to submit
                var loginForm = $(this).parents('form:first');
                if (loginForm !== undefined && loginForm !== null) {
                    loginForm.submit();
                }
            });
        },

        //create the function to submit the registration form
        submitRegistrationForm: function () {

            $("#submitRegisterButton").click(function (event) {
                event.preventDefault();
                //load the spinner
                $.busyLoadFull("show", {
                    background: "rgba(0, 0, 0, 0.21)",
                    spinner: "circles",
                    animation: "slide",
                    text: "CREATING YOUR ACCOUNT ...",
                    textPosition: "bottom"
                });
                //get the form to submit
                var productForm = $(this).parents('form:first');
                if (productForm !== undefined && productForm !== null) {
                    productForm.submit();
                }
            });
        }

    };

    // on doc ready load the defined main function
    $(document).ready(function () {
        siteScripts.onReady();
    });

})(jQuery);

