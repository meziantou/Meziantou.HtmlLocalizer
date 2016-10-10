# Meziantou.HtmlLocalizer

Generate localized html files.

# How does it work?

Create an html file and add the attribute `loc:name` on the tags you want to translate:

````html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <title></title>
</head>
<body>
    <h1 loc:name="Header">Header</h1>

    <p loc:name="Description">Description</p>
</body>
</html>
````

Run the tool to extract fields:

````cmd
dotnet htmllocalizer localization.json /extractOnly:true
````

Then add the translations in the json file:

````json
  [
    {
      "Path": "Sample.html",
      "Fields": [
        {
          "Name": "Header",
          "Values": {
            "innerText": {
              "": "Header",
              "fr": "Titre",
              "es": "título"
            }
          }
        },
        {
          "Name": "Description",
          "Values": {
            "innerText": {
              "": "Description",
              "fr": "Description",
              "es": "Descripción"
            }
          }
        }
      ]
    }
  ]
````

Run the tool to generate localized html files:

````cmd
dotnet htmllocalizer localization.json
````

An html file is generated for each language. For instance in French:

````html
<!DOCTYPE html><html><head>
    <meta charset="utf-8">
    <title></title>
</head>
<body>
    <h1>Titre</h1>
    <p>Description</p>
</body></html>
````

Of course you can re-extract fields without losing your translations.
