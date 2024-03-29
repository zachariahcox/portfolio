// Tools for exporting and rebalancing portfolios in google sheets.
// a google apps script to rebalance a portfolio and produce sheets describing the process. 
// the `positions`, `securities`, and `parameters` sheets need to have 1 frozen row. 
// these will be used as headers and should be reasonable json keys. 
//
// @zachariahcox

// main
function rebalancePortfolio(e) {
    // clear old recommendation
    deleteRebalanceSheets();
  
    // call rebalance service to get report object
    var portfolio = createPortfolio();
    var rebalanceService = portfolio.rebalanceparameters.url;
    var options = {
        'method' : 'post',
        'contentType': 'application/json',
        'payload' : JSON.stringify(portfolio)
    };
    var response = UrlFetchApp.fetch(rebalanceService, options);
    var responseJson = response.getContentText();
    var report = JSON.parse(responseJson);

    // add all sheets
    var ss = SpreadsheetApp.getActiveSpreadsheet();
    Object.keys(report).map(k => createTableSheet(ss, report, k));
}

function exportPortfolio(e) {
    var portfolio = createPortfolio();
    var json = JSON.stringify(portfolio, null, 4);
    displayText_(json, "Exported Portfolio");
}

function createTableSheet(ss, report, propertyName) {
    var s = ss.insertSheet("rebalance_" + propertyName, ss.getSheets().length);
    if (report.hasOwnProperty(propertyName)){
        var table = report[propertyName];
        if (table !== null)
        {
            var headerRowValues = Object.keys(table[0]);
            var rowCount = table.length;
            var colCount = headerRowValues.length;
            var _data = [];
            _data.push(headerRowValues);
            table.map(c => _data.push(Object.values(c)));
            s.getRange(1, 1, rowCount + 1, colCount).setValues(_data);
            
            // header formatting
            var headerRow = s.getRange(1, 1, 1, colCount);
            headerRow
                .setHorizontalAlignment("center")
                .setBackground("#d9e1f2")
                .setFontWeight("bold");
            
            // rows
            var colIndex = 1;
            for(const h of headerRowValues) {
                var l = h.toLowerCase();
                var format = l.includes("value")
                    ? "$#,##0"
                    : l.includes("percent")
                        ? "0.0%"
                        : "@";
                s.getRange(2, colIndex, rowCount, 1).setNumberFormat(format);
                colIndex += 1;
            }

            // customise sheet
            // s.autoResizeColumns(1, colCount); // this call is really slow?
            s.setTabColor("#57bb8a");
        }
    }
}

// delete everything that starts with our prefix
function deleteRebalanceSheets() { 
    var ss = SpreadsheetApp.getActiveSpreadsheet();
    ss.getSheets().map(n => {
        if (n.getName().startsWith("rebalance")){
            ss.deleteSheet(n);
        }
    });
}

// parse spreadsheet to create data object
function createPortfolio() {
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

    // grab rebalance parameters
    var params = getRowsData_(ss.getSheetByName('parameters'))[0];

    // create portfolio object
    var portfolio = {};
    portfolio["accounts"] = accounts;
    portfolio["securities"] = securityData;
    portfolio["rebalanceparameters"] = params;
    return portfolio;
}

function displayText_(text, title) {
    var output = HtmlService.createHtmlOutput("<textarea style='width:100%;' rows='20'>" + text + "</textarea>");
    output.setWidth(400);
    output.setHeight(300);
    SpreadsheetApp.getUi().showModalDialog(output, title);
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
    return char >= 'A' && char <= 'Z'
        || char >= 'a' && char <= 'z' 
        || isDigit_(char);
}

// Returns true if the character char is a digit, false otherwise.
function isDigit_(char) {
    return char >= '0' && char <= '9';
}

// register menu buttons
function onOpen() {
    var menuEntries = [
        { name: "Update Rebalance Sheets", functionName: "rebalancePortfolio" },
        { name: "Remove Rebalance Sheets", functionName: "deleteRebalanceSheets" },
        { name: "Export as json", functionName: "exportPortfolio"},
    ];
    SpreadsheetApp
        .getActiveSpreadsheet()
        .addMenu("Portfolio Tools", menuEntries);
}