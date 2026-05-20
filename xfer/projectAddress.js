//remmoved the following section since we do not have access to BST to update
//removed on 1/31/2025 by Bijan, approved by Bijan and Stephen
// let admin = "@isAdmin";
// document.onreadystatechange = function () {
//     if (document.readyState == "complete" && admin == "True") {


//         let readOnlyList = document.querySelectorAll("input[data-readonly='1']");

//         readOnlyList.forEach(function (item) {
//             item.readOnly = false;
//         });

//     }
// }



var projectId = document.getElementById('ProjectId');
let modalProjectId = document.getElementById('modalProjectId');
let modalAddress1 = document.getElementById('modalAddress1');
let modalAddress2 = document.getElementById('modalAddress2');
let modalCity = document.getElementById('modalCity');
let modalPostalCode = document.getElementById('modalPostalCode');
let modalState = document.getElementById('modalState');
let modalCountry = document.getElementById('modalCountry');
let countryId = 0;
let stateId = 0;




function ChangeCountry() {

    var srcField = "CountryId";
    var destinField = "StateId";

    const dropdown = document.getElementById(destinField);
    const id = $("#" + srcField + " :selected").val();

    const postUrl = ControllerPath("Projects", "GetStates",id);


    if (id !== "") {
        $.post(postUrl, function (data) {
            if (data.success === "true") {

                $(destinField).disabled = false;
                $("#" + destinField).empty();

                let option;
                option = document.createElement('option');
                option.text = "--Select State--";
                option.value = "";
                option.selected = true;
                option.disabled = true;
                dropdown.add(option);


                option = document.createElement('option');
                option.text = "--Unknown--";
                option.value = "1";
                dropdown.add(option);

                var selData = JSON.parse(data.data);
                for (let i = 0; i < selData.length; i++) {
                    option = document.createElement('option');
                    option.text = selData[i].Value;
                    option.value = selData[i].Key;
                    dropdown.add(option);


                }
            } else {
                alert("Error getting data!");
            }
        });
    } else {
        //Let's clear the values and disable :)
        $(destinField).disabled = true;
    }
    return false;

}


function GetUpdates() {

    //get the updates
    modalProjectId.value = projectId.value;

    let formData = new FormData();

    formData.append("projectId", projectId.value);
    formData.append("Token", $('input[name ="__RequestVerificationToken"]').val());

    var postUrl = ControllerPath("Datawarehouse", "GetAddress");

    fetch(postUrl, { method: "POST", body: formData })
        .then(response => response.json())
        .then(data => UpdateModal(data));

    $('#modalUpdates').modal("show");

}


function UpdateModal(data) {

    if (data['success'] == false) {
        return false;
    }


    if (data['project'] == NaN) {
        alert("Invalid Project Info received");
        return false;
    }

    var project = data['project'];

    modalAddress1.value = project['street'];
    //  modalAddress2.value = project['Street2'];
    modalCity.value = project['city'];
    modalPostalCode.value = project['zipPostalCode'];
    modalState.value = project['provinceName'];
    modalCountry.value = project['countryName'];

    countryId = data['country']['id'];
    stateId = data['state']['id'];

    document.getElementById("updateButton").innerText = 'Accept the change';


}

function updateFrm() {
    document.getElementById('CountryId').value = countryId;
    document.getElementById('StateId').value = stateId;
    document.getElementById('Address1').value = modalAddress1.value;
    document.getElementById('Address2').value = modalAddress2.value;
    document.getElementById('City').value = modalCity.value;
    document.getElementById('PostalCode').value = modalPostalCode.value;
    document.getElementById('Country').value = modalCountry.value;
    document.getElementById('State').value = modalState.value;

    $('#modalUpdates').modal("hide");

    alert("Please click on update to save the changes!");
}
