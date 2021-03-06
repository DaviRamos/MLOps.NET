## ⚡ MLOps.NET
[![Join the chat at https://gitter.im/aslotte/mlops.net](https://badges.gitter.im/aslotte/mlops.net.svg)](https://gitter.im/aslotte/mlops.net?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) ![.NET Core](https://github.com/aslotte/MLOps.NET/workflows/.NET%20Core/badge.svg) ![mlops.neticon](https://img.shields.io/nuget/v/MLOps.NET.svg)

MLOps.NET is a data science tool to track and manage the lifecycle of a [ML.NET](https://github.com/dotnet/machinelearning) machine learning model.

- Experiment tracking (SQLite, SQLServer, CosmosDb)
  - Experiments
  - Runs
  - Training time
  - Evaluation metrics
  - Hyper parameters
- Data tracking
  - Data schema
  - Data quantity
  - Data hash
  - Data distribution
- Model repostiory (Azure Blob Storage, AWS S3, local)
  - Run artifacts
  - Versioned registered models
- Model deployment (Azure Blob Storage, AWS S3, local)
  - URI based deployment
  - Containerized deployment (in roadmap)
  - Manual deployment (in roadmap)
  
A client application to vizualize and manage the ML lifecycle is currently in the [roadmap](https://github.com/aslotte/MLOps.NET/blob/master/images/roadmap.png) to be worked on.

### Getting started

`MLOps.NET` revolves around an `MLOpsContext`. The `MLOpsContext` contains catalogs for e.g.`Lifecycle`, `Data`, `Training`, `Evaluation` and `Deployment` to access operations helpful to manage your model's lifecycle.

To create an `MLOpsContext`, use the `MLOpsBuilder` with your desired configuration. You can mix and match the location of your model repository and metadata store as you please.

#### Azure with CosmosDb
```
  IMLOpsContext mlOpsContext = new MLOpsBuilder()
    .UseCosmosDb("accountEndPoint", "accountKey")
    .UseAzureBlobModelRepository("connectionString")
    .Build();
```

#### SQL Server with Local model repository
```
  IMLOpsContext mlOpsContext = new MLOpsBuilder()
    .UseSQLServer("connectionString")
    .UseLocalFileModelRepository()
    .Build();
```

#### AWS with SQLite
```
  IMLOpsContext mlOpsContext = new MLOpsBuilder()
    .UseSQLite()
    .UseAWSS3ModelRepository("awsAccessKey", "awsSecretAccessKey", "regionName")
    .Build();
```

#### Experiment tracking
To manage the lifecycle of a model, we'll need to track things such as the model's evaluation metrics, hyper-parameters used during training and so forth. We organize this under the concept of experiments and runs. An experiment is the logical grouping of a model we are trying to develop, e.g. a fraud classifier or recommendation engine. For a given experiment, we can create a number of runs. Each run represents one attempt to train a given model, which is associated with the run conditions and evaluation metrics achieved. 

To create an `Experiment` and a `Run`, access the `Lifecycle` catalog on the `MLOpsContext`
```
  var experimentId = await mlOpsContext.LifeCycle.CreateExperimentAsync();

  var run = await mlOpsContext.LifeCycle.CreateRunAsync(experimentId, "{optional Git SHA}");
```

For simplicity, you can also create an experiment (if it does not yet exist) and a run in one line
```
  var run = await mlOpsContext.LifeCycle.CreateRunAsync(experimentName: "FraudClassifier", "{optional Git SHA}");
```

With an `Experiment` and a `Run` created, we can track the model training process.

##### Hyperparameters
You can access the operations necessary to track hyperparameters on the `Training` catalog. You can either track individual hyperparameters, such as number of epocs as follows:
```
  await mlOpsContext.Training.LogHyperParameterAsync(runId, "NumberOfEpochs", epocs);
```

Alternatively, you can pass in the entire appended trainer and MLOps.NET will automatically log all of the trainer's hyperparameters for you
```
  await mlOpsContext.Training.LogHyperParameterAsync<SdcaLogisticRegressionBinaryTrainer>(runId, trainer);
```

##### Evaluation metrics
You can access the operations necessary to track evaluation metrics on the `Evaluation` catalog. Similarly to tracking hyperparameters, you can either log individual evaluation metrics as follows:
```
  await mlOpsContext.Evaluation.LogMetricAsync(runId, "F1Score", 0.99d);
```

Alternatively, you can pass the entire `ML.NET` evaluation metric result and `MLOps.NET` will log all related evaluation metrics for you automatically.
```
  await mlOpsContext.Evaluation.LogMetricsAsync<CalibratedBinaryClassificationMetrics>(runId, metric);
```


#### Data tracking
There are a number of useful methods on the `Data` catalog to track the data used for training. This will give you a nice audit trail to understand what data was used to train a specific model, as well as how the data looked and if it has changed in between models.

To log the data schema and the data hash (to be used to compare data for two different models), you can use the `LogDataAsync` method
```
  await mlOpsContext.Data.LogDataAsync(runId, dataView);
```

To log the distribution of a given column, e.g. how many rows in a given dataset are positive and how many are negative, use the `LogDistributionAsync` method

```
  await mlOpsContext.Data.LogDataDistribution<bool>(run.RunId, dataView, nameof(Review.Sentiment));
```

#### Model repository
The end product of any model development effort is the actual model itself. `MLOps.NET` offers the ability to store your model either in a storage account in Azure, an S3 bucket in AWS or locally on a fileshare of your choosing. 

To upload a model from a run
```
  var runArtifact = await mlOpsContext.Model.UploadAsync(runId, "pathToModel");
```

To register a model for deployment
```
  var registeredModel = await mlOpsContext.Model.RegisterModel(experimentId, runArtifact.RunArtifactId, registeredBy: "John Doe", description: "Altered weights");
```

#### Model deployment
Once a model has been registered, it's possible to deploy it to a given deployment target. A deployment target can be thought of as a specific environment in which you can serve your model, e.g. Test, Stage and Production. `MLOps.NET` currently supports serving a model via an URI or path, so that e.g. an ASP.NET Core application can consume the model. Similarily to the model repository, a deployment repository will be automatically created for you either in Azure Blob Storage, AWS S3 or on a local file share.

Methods to deploy a model can be found on the `Deployment` catalog. 
To deploy a model, start by creating a deployment target:

```
var deploymentTarget = await mlOpsContext.Deployment.CreateDeploymentTargetAsync(deploymentTargetName: "Test", isProduction: false);
```

Given a deployment target and a registered model, you can then deploy the model to a given environment:

```
  var deployment = await mlOpsContext.Deployment.DeployModelAsync(deploymentTarget, registeredModel, deployedBy: "John Doe");
```
The model is deployed to `deployment.DeploymentUri`, which can be used by a consuming application. It's also possible to get the URI/path to deployed model by doing the following:

```
  var deployment = await mlOpsContext.Deployment.GetDeployments()
    .FirstOrDefault(x => x.DeploymentTarget.Name == "Test");

  var deploymentUri = await mlOpsContext.Deployment.GetDeploymentUri(deployment);
```

Deploying a model for an experiment to a given deployment target, e.g. Test, will automatically overwrite the existing model, thus the consuming application will not need to update it's URI/path to the model it's consuming. `ML.NET` will automatically poll for changes to the file making it seamless and allowing the consuming application and the ML.NET model to have different release cycles.

We're actively working on supporting other deployment scenarios such as via Docker containers. 

## Contribute
We welcome contributors! Before getting started, take a moment to read our [contributing guidelines](https://github.com/aslotte/MLOps.NET/blob/master/Contributing.md) as well as the [docs for new developers](https://github.com/aslotte/MLOps.NET/wiki/Contributing-to-the-repository) on how to set up your local environment.

## Code of Conduct
Please take a moment to read our [code of conduct](https://github.com/aslotte/MLOps.NET/blob/master/CODE_OF_CONDUCT.md) 

