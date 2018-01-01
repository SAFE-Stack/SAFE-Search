# houseprice-sales

This is a SAFE F# project (Suave model, Azure, Fable, Elmish) designed to show integration of Azure Search and Storage within an Elmish application using Bootstrap.

## Running locally

To run locally, execute the contents of the `scripts\importdata.fsx` file before running the app. This will download the first 1000 transactions from the latest online property dataset and store it locally. Using a local file, the search experience is reduced and there is no support for postcode matching, but it is a quick way to use the app.

Then, run `build.cmd`.

### WARNING!
Many issues around data and azure need to be addressed (please see issues list). This will be updated in the coming days / weeks. In the meantime, there are a few up-for-grabs issues if someone does want to help out at this early stage.