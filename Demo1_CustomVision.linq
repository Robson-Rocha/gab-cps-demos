<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Net.Http.dll</Reference>
  <NuGetReference>Microsoft.Cognitive.CustomVision.Prediction</NuGetReference>
  <NuGetReference>Microsoft.Cognitive.CustomVision.Training</NuGetReference>
  <NuGetReference>Microsoft.Rest.ClientRuntime</NuGetReference>
  <Namespace>Microsoft.Cognitive.CustomVision.Prediction</Namespace>
  <Namespace>Microsoft.Cognitive.CustomVision.Prediction.Models</Namespace>
  <Namespace>Microsoft.Cognitive.CustomVision.Training</Namespace>
  <Namespace>Microsoft.Cognitive.CustomVision.Training.Models</Namespace>
</Query>

//Step 0 - Create the Custom Vison Service at Azure Portal, and grab the keys
const string trainingApiKey = "";
const string predictionApiKey = "";

void Main()
{
	//Step 1 - Get Training API instance and set API Key
	Console.WriteLine("Getting training API...");
	TrainingApi trainingApi = new TrainingApi
	{
		ApiKey = trainingApiKey
	};
	Console.WriteLine("done");

	//Step 2 - Get Domain Types and Select General Domain
	Console.WriteLine("Getting Domain Types...");
	IList<Domain> domains = trainingApi.GetDomains();
	domains.Dump();
	Domain generalDomain = domains.First(d => d.Name == "General" && d.Exportable == false);

	//Step 3 - Create a New Project
	Console.WriteLine("Checking if there is an existing Project...");
	IList<Project> projects = trainingApi.GetProjects();
	if(projects?.Any() ?? false)
	{
		Console.WriteLine("Deleting existing Projects...");
		foreach (var projectId in projects.Select(p => p.Id))
			trainingApi.DeleteProject(projectId);
	}
	Console.WriteLine("Creating a new Project...");
	Project project = trainingApi.CreateProject($"GABCPS_CustomVisionDemoProject_{Guid.NewGuid()}", 
												"CustomVision demonstration project for Global Azure Bootcamp Campinas", 
												generalDomain.Id);
	project.Dump();

	//Step 4 - Create a pair of tags
	Console.WriteLine("Creating Tags...");
	Tag tagRgFront = trainingApi.CreateTag(project.Id, "RG_Front", "RG Front");
	tagRgFront.Dump();
	Tag tagRgBack = trainingApi.CreateTag(project.Id, "RG_Back", "RG Back");
	tagRgBack.Dump();

	//Step 5 - Upload images for training   
	void uploadImages(Tag tag)
	{
		var tagIds = new List<string> { tag.Id.ToString() };
		foreach (var imagePath in Directory.GetFiles($@"{Path.GetDirectoryName(Util.CurrentQueryPath)}\DataSets\CustomVision\{tag.Name}"))
		{
			Console.Write($"Uploading '{imagePath}'... ");
			Util.Image(imagePath).Dump();
			using (var imageStream = new FileStream(imagePath, FileMode.Open))
				trainingApi.CreateImagesFromData(project.Id, imageStream, tagIds);
		}
	}
	Console.WriteLine("Uploading Images... ");
	uploadImages(tagRgFront);
	uploadImages(tagRgBack);
	Console.WriteLine("Images uploaded");

	//Step 6 - Train the project!
	Console.WriteLine("Training iteration... ");
	Iteration iteration = trainingApi.TrainProject(project.Id);
	iteration.Dump();
	while (iteration.Status == "Training")
	{
		Console.Write(".");
		iteration = trainingApi.GetIteration(project.Id, iteration.Id);
		iteration.Dump();
	}
	Console.WriteLine("Setting iteration as default... ");
	iteration.IsDefault = true;
	trainingApi.UpdateIteration(project.Id, iteration.Id, iteration);

	//Step 8 - Make Predictions
	Console.WriteLine("Make predictions... ");
	PredictionEndpoint predictionEndpoint = new PredictionEndpoint()
	{
		ApiKey = predictionApiKey
	};
	foreach (var imagePath in Directory.GetFiles($@"{Path.GetDirectoryName(Util.CurrentQueryPath)}\DataSets\CustomVision\Test"))
	{
		Console.WriteLine($"\r\n{imagePath}:");
		Util.Image(imagePath).Dump();
		ImagePredictionResultModel result;
		using (var imageStream = new FileStream(imagePath, FileMode.Open))
			result = predictionEndpoint.PredictImage(project.Id, imageStream);//, iteration.Id);

		foreach (ImageTagPredictionModel predictedTag in result.Predictions)
			Console.WriteLine($"\t{predictedTag.Tag}: {predictedTag.Probability:P2}");
	}

}

// Define other methods and classes here