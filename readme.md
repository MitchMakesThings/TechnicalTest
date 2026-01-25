## BSPOKE Software Technical Assessment

This is my submissino for the tech test. Original instructions are in `readme.instructions.md`

In addition to the code itself, I tried to do regular fairly-small commits. These could be reviewed to get an idea of how I approached the project.

Obviously there are many things that could be improved, but I'm reasonably happy with the time/functionality trade-off.

### Core assumptions
1. From the mention of a mobile app, I've assumed this API is primarily intended for a customer-facing app. Additional admin(bank staff)-specific endpoints have been added for non-customer behaviour (creating new customers, generating auth tokens etc) to assist with testing.
1. Account management & authentication is out of scope. It's assumed there would be a service authenticating users and producing a JWT the mobile app would send as part of a Bearer authentication header. \
An endpoint (POST /api/admin/customers/{customerId}) has been provided for testing, which will simulate login and generate a valid JWT.
1. While the requirements were that customers should be able to perform transfers between their own accounts, I assumed there was no restriction against transfering to other accounts. So there is no my-account-only restriction currently, though this could easily be added as another CreditAccountNotFound error.
1. Data should not really be deleted. The repositories handle soft-deleting entities, and ensuring we don't retrieve deleted entities in normal operation.
1. A single transaction log entry per transaction is sufficient for the tech test. I'm no accountant, but I guess a real prototype would use double-entry book-keeping.
1. Further assumptions are listed in the code where relevant, with an ASSUMPTION: prefix to the comments.


### Usage
Run the project as normal. Migrations will apply to generate (or upgrade) the database.

The AdminCustomers endpoints can be used as if you were a bank staff member. Use the POST /api/admin/customers endpoint to create new customers - complete with their initial balance and daily limit.

For all other endpoints you will need to be authorized.
Use the POST /api/admin/customers/{customerId} endpoint to generate a JWT token to log in as that customer. Use Swagger UI's copy-response button to copy the token. At the top right of Swagger UI is an Authorize button, paste the token in there.
Now all requests you make to the non-AdminCustomer endpoints will be in the context of the selected customer.


### Structure
I've taken a pretty standard Controller -> Model -> Repository approach.

All business logic (including validations) have been implemented in the Modules. Data access is via repositories, and the controllers are just dumb IO.
I'm also using the same DTO/Model objects for the Controller and Module layers.

I like the separation of concerns, and it would allow different teams to work on different projects simultaneously. Business rules (Modules) could be moved to a separate business logic project. There's no need for the Controllers to have direct references to repositories/application context.
This would also make it easier for eg, a data-access-layer team building caching, hot/warm/cold storage access etc, without needing to touch the layers above.

If the project were to continue to grow, I would also strongly consider moving validation out of the main module methods. Controllers could call validation methods, handle any DTO->business layer model translation etc, then call functional methods.

I experimented with ProblemDetails as a response format for errors during development, but wound up returning to a tried-and-true standard ApiResponse object. This way the mobile app client will always have a standard format to parse. Endpoint-specific error response codes can be handled & displayed appropriately client-side.
I've tried to use standard HTTP status codes where appropriate.
There's also exception handling middleware for when we throw a 500, to ensure we still return the standard ApiResponse format.

Tests have been added in their own project. If I'd had time for the api tests I'd be inclined to have them in their own project as well.


### Tests
While the technical test itself has been written by hand, I wanted an excuse to trial AI so I had Jetbrains Junie write the unit test project. So most of the tests are AI generated, with small manual tweaks.

I am moderately impressed. It did a reasonable job of testing functionality as implemented, but obviously didn't do any thinking about additional edge cases. See the last few commits for examples of what occurred to me while reviewing the generated tests. AI definitely won't be taking over QA any time soon.

Since the technical test is also using SQLite, the tests are running with an in-memory SQLite database. However on a larger scale project I would expect to use something like TestContainers to spin up an appropriate (MSSql/Postgres etc) database.

I also haven't had time to do tests for the api endpoints. But for most projects I have used WebApplicationFactory (again, often backed by TestContainers) to spin up the full asp.net core pipeline for end-to-end tests.