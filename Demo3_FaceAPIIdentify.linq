<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Net.Http.dll</Reference>
  <NuGetReference>Microsoft.ProjectOxford.Face</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>Microsoft.ProjectOxford.Face</Namespace>
  <Namespace>Microsoft.ProjectOxford.Face.Contract</Namespace>
</Query>


const string personGroupId = "palestrantes";
const string personGroupName = "Palestrantes Azure Bootcamp Campinas";
readonly string definitionsPath = $@"{Path.GetDirectoryName(Util.CurrentQueryPath)}\DataSets\FaceAPIIdentify\Definitions";
readonly string testsPath = $@"{Path.GetDirectoryName(Util.CurrentQueryPath)}\DataSets\FaceAPIIdentify\Test";

//Step 0: Create the Face API Service at Azure Portal, and grab the key and endpoint
const string subscriptionKey = "";
const string apiRoot = "";
void Main()
{
	var faceServiceClient = new FaceServiceClient(subscriptionKey, apiRoot);

	//Step 1: Create the Person Group
	Console.WriteLine("Checking if there is an existing person group...");
	PersonGroup personGroup = null;
	try
	{
		personGroup = faceServiceClient.GetPersonGroupAsync(personGroupId).Result;
		faceServiceClient.DeletePersonGroupAsync(personGroupId).Wait();
	}
	catch
	{ }
	Console.WriteLine($"Creating person group {personGroupId}... ");
	faceServiceClient.CreatePersonGroupAsync(personGroupId, personGroupName).Wait();

	//Step 2: Add Persons to the Group
	Console.WriteLine($"Filling the person group... ");
	foreach (var imagePath in Directory.GetFiles(definitionsPath))
	{
		string personName = Path.GetFileNameWithoutExtension(imagePath);
		Console.WriteLine($"Creating person {personName}... ");
		Util.Image(imagePath).Dump();
		CreatePersonResult person = faceServiceClient.CreatePersonAsync(personGroupId, personName).Result;

		Console.WriteLine($"Storing {personName} face... ");
		using (Stream imageStream = File.OpenRead(imagePath))
			faceServiceClient.AddPersonFaceAsync(personGroupId, person.PersonId, imageStream).Wait();
	}

	//Step 3: Train the Person Group
	Console.WriteLine($"Training the person group... ");
	faceServiceClient.TrainPersonGroupAsync(personGroupId).Wait();
	while (true)
	{
		if (faceServiceClient.GetPersonGroupTrainingStatusAsync(personGroupId).Result.Status != Status.Running)
			break;
		Thread.Sleep(1000);
	}

	//Step 4: Detect and identify persons in the group
	Console.WriteLine($"Looking for known faces... ");
	foreach (var imagePath in Directory.GetFiles(testsPath))
	{
		Console.WriteLine($"Detecting faces in '{imagePath}'... ");
		byte[] imageData = File.ReadAllBytes(imagePath);
		Util.Image(imageData).Dump();
		Face[] faces = faceServiceClient.DetectAsync(new MemoryStream(imageData),
									  returnFaceId: true).Result;
		faces.Dump();

		Console.WriteLine($"Identifying faces in '{imagePath}'... ");
		IdentifyResult[] results = faceServiceClient.IdentifyAsync(
										personGroupId, faces.Select(f => f.FaceId).ToArray()).Result;
		foreach (IdentifyResult result in results)
		{
			foreach (Candidate candidate in result.Candidates)
			{
				Guid personId = candidate.PersonId;
				Person person = faceServiceClient.GetPersonAsync(personGroupId, personId).Result;
				person.Dump();
				Util.Image($@"{definitionsPath}\{person.Name}.jpg").Dump();
			}
		}
	}
}

// Define other methods and classes here
