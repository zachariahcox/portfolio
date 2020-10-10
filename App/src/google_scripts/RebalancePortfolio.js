// export a custom view of the user input data
// the positions and securities sheets need to have 1 frozen row. 
// these will be used as headers and should be reasonable json keys. 
//

// main export function
function rebalancePortfolio(e) {
    var ss = SpreadsheetApp.getActiveSpreadsheet();

    // aggregate positions into accounts
    var accounts = [];
    var positionData = getRowsData_(ss.getSheetByName('positions'));
    for (const r of positionData) {
        if (r.value <= 0) {
            continue; // not very interesting!
        }
        var a = accounts.find(element => element.name === r.account);
        if (a === undefined) {
            // create new account
            a = {
                name: r.account,
                brokerage: r.brokerage,
                type: r.type, 
                positions: []
            };
            accounts.push(a);
        }

        // add new position
        a.positions.push({
            symbol: r.symbol,
            quantity: r.quantity,
            value: r.value,
            hold: r.hold,
            description: r.description
        });
    }

    // cleanup security data
    var securityData = getRowsData_(ss.getSheetByName('securities'));
    securityData = securityData.filter(x => x.hasOwnProperty("symbol"));
    for(const s of securityData) {
        if (s.symbol === s.symbolmap) {
            delete s.symbolmap; // no need for both if they match
            delete s.quantitymap;
        }
    }

    // create portfolio object
    var portfolio = {};
    portfolio["accounts"] = accounts;
    portfolio["securities"] = securityData;

    // rebalance using service
    var rebalanceService = "http://52.154.202.11/rebalance";
    var options = {
        'method' : 'post',
        'contentType': 'application/json',
        'payload' : JSON.stringify(portfolio)
    };
    var response = UrlFetchApp.fetch(rebalanceService, options);
    displayText_(response.getContentText());

    // print to sheet
    var responseJson = "{\"Composition\":[{\"Class\":\"*\",\"Location\":\"*\",\"Value\":12.56,\"TotalPercent\":100,\"ClassPercent\":100,\"LocationPercent\":100,\"Brokerage\":0,\"Ira\":0,\"Roth\":100},{\"Class\":\"*\",\"Location\":\"domestic\",\"Value\":12.56,\"TotalPercent\":100,\"ClassPercent\":100,\"LocationPercent\":100,\"Brokerage\":0,\"Ira\":0,\"Roth\":100},{\"Class\":\"*\",\"Location\":\"international\",\"Value\":0,\"TotalPercent\":0,\"ClassPercent\":0,\"LocationPercent\":0,\"Brokerage\":0,\"Ira\":0,\"Roth\":0},{\"Class\":\"stock\",\"Location\":\"*\",\"Value\":12.56,\"TotalPercent\":100,\"ClassPercent\":100,\"LocationPercent\":100,\"Brokerage\":0,\"Ira\":0,\"Roth\":100},{\"Class\":\"stock\",\"Location\":\"domestic\",\"Value\":12.56,\"TotalPercent\":100,\"ClassPercent\":100,\"LocationPercent\":100,\"Brokerage\":0,\"Ira\":0,\"Roth\":100},{\"Class\":\"stock\",\"Location\":\"international\",\"Value\":0,\"TotalPercent\":0,\"ClassPercent\":0,\"LocationPercent\":0,\"Brokerage\":0,\"Ira\":0,\"Roth\":0},{\"Class\":\"bond\",\"Location\":\"*\",\"Value\":0,\"TotalPercent\":0,\"ClassPercent\":0,\"LocationPercent\":0,\"Brokerage\":0,\"Ira\":0,\"Roth\":0},{\"Class\":\"bond\",\"Location\":\"domestic\",\"Value\":0,\"TotalPercent\":0,\"ClassPercent\":0,\"LocationPercent\":0,\"Brokerage\":0,\"Ira\":0,\"Roth\":0},{\"Class\":\"bond\",\"Location\":\"international\",\"Value\":0,\"TotalPercent\":0,\"ClassPercent\":0,\"LocationPercent\":0,\"Brokerage\":0,\"Ira\":0,\"Roth\":0}],\"Comparison\":[{\"Class\":\"*\",\"Location\":\"*\",\"Value\":0,\"TotalPercent\":0,\"ClassPercent\":0,\"LocationPercent\":0,\"Brokerage\":0,\"Ira\":0,\"Roth\":0},{\"Class\":\"*\",\"Location\":\"domestic\",\"Value\":0,\"TotalPercent\":0,\"ClassPercent\":0,\"LocationPercent\":0,\"Brokerage\":0,\"Ira\":0,\"Roth\":0},{\"Class\":\"*\",\"Location\":\"international\",\"Value\":0,\"TotalPercent\":0,\"ClassPercent\":0,\"LocationPercent\":0,\"Brokerage\":0,\"Ira\":0,\"Roth\":0},{\"Class\":\"stock\",\"Location\":\"*\",\"Value\":0,\"TotalPercent\":0,\"ClassPercent\":0,\"LocationPercent\":0,\"Brokerage\":0,\"Ira\":0,\"Roth\":0},{\"Class\":\"stock\",\"Location\":\"domestic\",\"Value\":0,\"TotalPercent\":0,\"ClassPercent\":0,\"LocationPercent\":0,\"Brokerage\":0,\"Ira\":0,\"Roth\":0},{\"Class\":\"stock\",\"Location\":\"international\",\"Value\":0,\"TotalPercent\":0,\"ClassPercent\":0,\"LocationPercent\":0,\"Brokerage\":0,\"Ira\":0,\"Roth\":0},{\"Class\":\"bond\",\"Location\":\"*\",\"Value\":0,\"TotalPercent\":0,\"ClassPercent\":0,\"LocationPercent\":0,\"Brokerage\":0,\"Ira\":0,\"Roth\":0},{\"Class\":\"bond\",\"Location\":\"domestic\",\"Value\":0,\"TotalPercent\":0,\"ClassPercent\":0,\"LocationPercent\":0,\"Brokerage\":0,\"Ira\":0,\"Roth\":0},{\"Class\":\"bond\",\"Location\":\"international\",\"Value\":0,\"TotalPercent\":0,\"ClassPercent\":0,\"LocationPercent\":0,\"Brokerage\":0,\"Ira\":0,\"Roth\":0}],\"Positions\":[{\"Account\":\"test\",\"Symbol\":\"fzrox\",\"Url\":null,\"Value\":12.56,\"Description\":\"domestic stock?\"}],\"Orders\":null}";
    var rebalanceInstructions = JSON.parse(responseJson);
    var rebalanceSheet = ss.insertSheet("rebalance", ss.getSheets().length);
    if (rebalanceInstructions.hasOwnProperty("Composition")){
        var comp = rebalanceInstructions.Composition;
        if (comp !== null)
        {
            s.appendRow(Object.keys(comp[0])); // randomly pick one
            comp.map(c => rebalanceSheet.appendRow(Object.values(c)));
        }
    }
}

function displayText_(text) {
    var output = HtmlService.createHtmlOutput("<textarea style='width:100%;' rows='20'>" + text + "</textarea>");
    output.setWidth(400);
    output.setHeight(300);
    SpreadsheetApp.getUi().showModalDialog(output, 'Exported Portfolio');
}

// getRowsData iterates row by row in the input range and returns an array of objects.
// Each object contains all the data for a given row, indexed by its normalized column name.
// Arguments:
//   - sheet: the sheet object that contains the data to be processed
function getRowsData_(sheet) {
    var headersRange = sheet.getRange(1, 1, sheet.getFrozenRows(), sheet.getMaxColumns());
    var headers = headersRange.getValues()[0];
    var dataRange = sheet.getRange(sheet.getFrozenRows() + 1, 1, sheet.getMaxRows(), sheet.getMaxColumns());
    var objects = getObjects_(dataRange.getValues(), normalizeHeaders_(headers));
    return objects;
}

// For every row of data in data, generates an object that contains the data. Names of
// object fields are defined in keys.
// Arguments:
//   - data: JavaScript 2d array
//   - keys: Array of Strings that define the property names for the objects to create
function getObjects_(data, keys) {
    var objects = [];
    for (var i = 0; i < data.length; ++i) {
        var object = {};
        var hasData = false;
        for (var j = 0; j < data[i].length; ++j) {
            var cellData = data[i][j];
            if (isCellEmpty_(cellData)) {
                continue;
            }
            object[keys[j]] = cellData;
            hasData = true;
        }
        if (hasData) {
            objects.push(object);
        }
    }
    return objects;
}

// Returns an Array of normalized Strings.
// Arguments:
//   - headers: Array of Strings to normalize
function normalizeHeaders_(headers) {
    var keys = [];
    for (var i = 0; i < headers.length; ++i) {
        var key = normalizeHeader_(headers[i]);
        if (key.length > 0) {
            keys.push(key);
        }
    }
    return keys;
}

// Normalizes a string, by removing all alphanumeric characters and using mixed case
// to separate words. The output will always start with a lower case letter.
// This function is designed to produce JavaScript object property names.
// Arguments:
//   - header: string to normalize
// Examples:
//   "First Name" -> "firstName"
//   "Market Cap (millions) -> "marketCapMillions
//   "1 number at the beginning is ignored" -> "numberAtTheBeginningIsIgnored"
function normalizeHeader_(header) {
    var key = "";
    var upperCase = false;
    for (var i = 0; i < header.length; ++i) {
        var letter = header[i];
        if (letter == " " && key.length > 0) {
            upperCase = true;
            continue;
        }
        if (!isAlnum_(letter)) {
            continue;
        }
        if (key.length == 0 && isDigit_(letter)) {
            continue; // first character must be a letter
        }
        if (upperCase) {
            upperCase = false;
            key += letter.toUpperCase();
        } else {
            key += letter.toLowerCase();
        }
    }
    return key;
}

// Returns true if the cell where cellData was read from is empty.
// Arguments:
//   - cellData: string
function isCellEmpty_(cellData) {
    return typeof (cellData) == "string" && cellData == "";
}

// Returns true if the character char is alphabetical, false otherwise.
function isAlnum_(char) {
    return char >= 'A' && char <= 'Z' ||
        char >= 'a' && char <= 'z' ||
        isDigit_(char);
}

// Returns true if the character char is a digit, false otherwise.
function isDigit_(char) {
    return char >= '0' && char <= '9';
}

// register menu button
function onOpen() {
    var menuEntries = [
        { name: "Update Rebalance Sheet", functionName: "rebalancePortfolio" }
    ];
    SpreadsheetApp
        .getActiveSpreadsheet()
        .addMenu("Rebalance Portfolio", menuEntries);
}