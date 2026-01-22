# SURIMI.Controller


## reading the contract yaml file
The contract of a SURIMI Simulation is defined in a yaml file. For example the "western_med_contract.yaml" file.
In the Initialise message the controller will try to read the contract yaml file from the "/Includes" directory. 

If it can't find the file, the program assumes it is running as a docker image and will try to read the file from the S3 bucket.
If it can find it, it's running locally.


### Retrieve files from S3
Retrieve files from S3 is done using MinIO Client.

You initialise the MinIO Client with the following Environment Variables:

- AWS_S3_ENDPOINT
- AWS_DEFAULT_REGION
- AWS_BUCKET_NAME
- AWS_ACCESS_KEY_ID
- AWS_SECRET_ACCESS_KEY
- AWS_SESSION_TOKEN

The Session Token is is valid for 24 hours.
After that you need to get a new token.

### Refreshing the Session Token
