# houseprice-sales

This is a SAFE F# project (Suave model, Azure, Fable, Elmish) designed to show integration of Azure Search and Storage within an Elmish application using Bootstrap.

## Running locally

To run locally, execute the contents of the `scripts\importdata.fsx` file before running the app. This will download the first 1000 transactions from the latest property data and store it locally.

### WARNING!

This is not really ready for consumption. There's no build script yet, and many other issues around data and azure need to be addressed (please see issues list). This will be updated in the coming days / weeks.

In the meantime, there are a few up-for-grabs issues if someone does want to help out at this early stage.