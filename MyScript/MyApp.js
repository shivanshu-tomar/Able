var app = angular.module("myApp", []);
app.controller("theamControler", function ($scope, myService) {
    $scope.logout = () => {
        console, log("its logging out")
    }
});
app.filter('unique', function ()
{
    return function (items, filterOn)
    {
        if (filterOn === false)
        {
            return items;
        }
        if ((filterOn || angular.isUndefined(filterOn)) && angular.isArray(items))
        {
            var hashCheck = {}, newItems = [];

            var extractValueToCompare = function (item)
            {
                if (angular.isObject(item) && angular.isString(filterOn))
                {
                    return item[filterOn];
                } else
                {
                    return item;
                }
            };

            angular.forEach(items, function (item)
            {
                var valueToCheck, isDuplicate = false;

                for (var i = 0; i < newItems.length; i++)
                {
                    if (angular.equals(extractValueToCompare(newItems[i]), extractValueToCompare(item)))
                    {
                        isDuplicate = true;
                        break;
                    }
                }
                if (!isDuplicate)
                {
                    newItems.push(item);
                }
            });
            items = newItems;
        }
        return items;
    };
});
app.filter('capitalize', function ()
{
    return function (input)
    {
        if (input.indexOf(' ') !== -1)
        {
            var inputPieces,
                i;

            input = input.toLowerCase();
            inputPieces = input.split(' ');

            for (i = 0; i < inputPieces.length; i++)
            {
                inputPieces[i] = capitalizeString(inputPieces[i]);
            }

            return inputPieces.toString().replace(/,/g, ' ');
        }
        else
        {
            input = input.toLowerCase();
            return capitalizeString(input);
        }

        function capitalizeString(inputString)
        {
            return inputString.substring(0, 1).toUpperCase() + inputString.substring(1);
        }
    };
});
app.directive('numbersOnly', function ()
{
    return {
        require: 'ngModel',
        link: function (scope, element, attr, ngModelCtrl)
        {
            function fromUser(text)
            {
                if (text)
                {
                    var transformedInput = text.replace(/[^0-9]/g, '');

                    if (transformedInput !== text)
                    {
                        ngModelCtrl.$setViewValue(transformedInput);
                        ngModelCtrl.$render();
                    }
                    return transformedInput;
                }
                return '';
            }
            ngModelCtrl.$parsers.push(fromUser);
        }
    };
});
app.directive('onlyDigits', function ()
{
    return {
        restrict: 'A',
        require: '?ngModel',
        link: function (scope, element, attr, ctrl)
        {
            function inputValue(val)
            {
                if (val)
                {
                    var digits = val.replace(/[^0-9.]/g, '');

                    if (digits !== val)
                    {
                        ctrl.$setViewValue(digits);
                        ctrl.$render();
                    }
                    return parseFloat(digits);
                }
                return '';
            }
            ctrl.$parsers.push(inputValue);
        }
    };
});
app.directive('number', function ()
{
    return {
        require: 'ngModel',
        restrict: 'A',
        link: function (scope, element, attrs, ctrl)
        {
            ctrl.$parsers.push(function (input)
            {
                if (input == undefined) return ''
                var inputNumber = input.toString().replace(/[^0-9]/g, '');
                if (inputNumber != input)
                {
                    ctrl.$setViewValue(inputNumber);
                    ctrl.$render();
                }
                return inputNumber;
            });
        }
    };
});
app.filter('titleCase', function ()
{
    return function (input)
    {
        input = input || '';
        return input.replace(/\w\S*/g, function (txt)
        {
            return txt.charAt(0).toUpperCase() + txt.substr(1).toLowerCase();
        });
    };
})
function ValidateSize(file)
{

    var fileName = document.getElementById(file.id).value.toLowerCase();
    if (!fileName.endsWith('.pdf'))
    {
        alert('Please upload pdf file only.');
        $(file).val('');
        return false;
    }
    var FileSize = file.files[0].size / 1024 / 1024; // in MB
    if (FileSize > 3)
    {
        alert('Your file should not larger than 3 MB');
        $(file).val(''); //for clearing with Jquery
        return false;
    }
}

$('.alphaonly').bind('keyup blur', function ()
{
    var node = $(this);
    node.val(node.val().replace(/[^A-Za-z_\s.]/, ''));
});

$('.alphanumberonly').bind('keyup blur', function ()
{
    var node = $(this);
    node.val(node.val().replace(/[^A-Za-z0-9]/, ''));
});

$('.alphaonlyusername').bind('keyup blur', function ()
{
    var node = $(this);
    node.val(node.val().replace(/[^glaGLAE0-9-]/, ''));
});

$('.emailname').bind('keyup blur', function ()
{
    var node = $(this);
    node.val(node.val().replace(/[^A-Za-z0-9._@@-]/, ''));
});

// allow only  Number 0 to 9
$('.numberonly').bind('keyup blur', function ()
{
    var node = $(this);
    node.val(node.val().replace(/[^0-9]/, ''));
});