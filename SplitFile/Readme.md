# AWS Lambda Empty Function Project

This starter project consists of:
* Function.cs - class file containing a class with a single function handler method
* aws-lambda-tools-defaults.json - default argument settings for use with Visual Studio and command line deployment tools for AWS

You may also have a test project depending on the options selected.

The generated function handler is a simple method accepting a string argument that returns the uppercase equivalent of the input string. Replace the body of this method, and parameters, to suit your needs. 

## Here are some steps to follow from Visual Studio:

To deploy your function to AWS Lambda, right click the project in Solution Explorer and select *Publish to AWS Lambda*.

To view your deployed function open its Function View window by double-clicking the function name shown beneath the AWS Lambda node in the AWS Explorer tree.

To perform testing against your deployed function use the Test Invoke tab in the opened Function View window.

To configure event sources for your deployed function, for example to have your function invoked when an object is created in an Amazon S3 bucket, use the Event Sources tab in the opened Function View window.

To update the runtime configuration of your deployed function use the Configuration tab in the opened Function View window.

To view execution logs of invocations of your function use the Logs tab in the opened Function View window.

## Here are some steps to follow to get started from the command line:

Once you have edited your template and code you can deploy your application using the [Amazon.Lambda.Tools Global Tool](https://github.com/aws/aws-extensions-for-dotnet-cli#aws-lambda-amazonlambdatools) from the command line.

Install Amazon.Lambda.Tools Global Tools if not already installed.
```
    dotnet tool install -g Amazon.Lambda.Tools
```

If already installed check if new version is available.
```
    dotnet tool update -g Amazon.Lambda.Tools
```

Execute unit tests
```
    cd "MergeFile/test/MergeFile.Tests"
    dotnet test
```

Deploy function to AWS Lambda
```
    cd "MergeFile/src/MergeFile"
    dotnet lambda deploy-function
```




aws s3api list-multipart-uploads --bucket processed-tasks-bucket-sf

aws s3api abort-multipart-upload --bucket processed-tasks-bucket-sf --key 27a4f6ae-c714-42ca-8e19-f56a42ff2a64..csv -upload-id _3.i7e.gUeP40dV3CugDzNe2L53qbDcaxxGMo672GP6hdk5dcAa6ZHgYF0MPnYGPJwo3Wf8l6YcyM8bVejquh_GCJpnP9_u3Kzu0_5YnE0yNJsmrFk_AV0G8WKSDvztS

aws s3api abort-multipart-upload --bucket processed-tasks-bucket-sf --key testfile_financial_data_1gb.csv --upload-id QAEIxTrdE2pofyTmuxuwOmpHQgC_t8ZvoLIeRvPbr02MlTJ173NRgqvNYl24tKhc8w_i2cVcqyqfH9cq8IUgRa2k5FOzdxL517OY5PtZDihq_0f1IyO3ja1qalq.uZ

aws s3api list-parts --bucket processed-tasks-bucket-sf --key 2feea9b8-3cb9-4bb9-9d43-fc0ffac52af3..csv --upload-id izYDlkpYUK53XyHbrQ5EeONE_YL8wCKLwUX3I3JBMxfwiLdC6IOOvNOQGR8nzo2CXrmkODFvwJ67qU1wBUGeAOIo3v6sYUOx7XhJyguCKV5E8ppYHcTZss05Fy3Agqcc

aws s3api complete-multipart-upload --multipart-upload file://mpustruct --bucket processed-tasks-bucket-sf --key '27a4f6ae-c714-42ca-8e19-f56a42ff2a64..csv' --upload-id _3.i7e.gUeP40dV3CugDzNe2L53qbDcaxxGMo672GP6hdk5dcAa6ZHgYF0MPnYGPJwo3Wf8l6YcyM8bVejquh_GCJpnP9_u3Kzu0_5YnE0yNJsmrFk_AV0G8WKSDvztS



aws s3api complete-multipart-upload --multipart-upload file://mpustruct --bucket processed-tasks-bucket-sf --key '27a4f6ae-c714-42ca-8e19-f56a42ff2a64..csv' --upload-id _3.i7e.gUeP40dV3CugDzNe2L53qbDcaxxGMo672GP6hdk5dcAa6ZHgYF0MPnYGPJwo3Wf8l6YcyM8bVejquh_GCJpnP9_u3Kzu0_5YnE0yNJsmrFk_AV0G8WKSDvztS

dotnet lambda deploy-function MergeFileFuntion --function-role splifile-role-0fhp0fas