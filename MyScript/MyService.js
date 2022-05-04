app.service("myService", function ($http) {
    this.UserLogin = function (User,Path) {
        var response = $http({
            method: "post",
            url: Path,
            data: JSON.stringify(User),
            dataType: "json"
        });
        return response;
    }
});