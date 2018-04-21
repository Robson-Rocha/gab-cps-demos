<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Net.Http.dll</Reference>
  <NuGetReference>Microsoft.ProjectOxford.Face</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>Microsoft.ProjectOxford.Face</Namespace>
  <Namespace>Microsoft.ProjectOxford.Face.Contract</Namespace>
</Query>

//Step 0: Create the Face API Service at Azure Portal, and grab the key and endpoint
const string subscriptionKey = "";
const string apiRoot = "";

void Main()
{
	var faceServiceClient = new FaceServiceClient(subscriptionKey, apiRoot);

	//Step 2: Detect the faces
	foreach (var imagePath in Directory.GetFiles($@"{Path.GetDirectoryName(Util.CurrentQueryPath)}\DataSets\FaceAPIDetect"))
	{
		Console.Write($"Detecting faces in '{imagePath}'... ");
		byte[] imageData = File.ReadAllBytes(imagePath);
		Util.Image(imageData).Dump();
		Face[] faces = faceServiceClient.DetectAsync(new MemoryStream(imageData),
									  returnFaceId: false,
									  returnFaceLandmarks: false,
									  returnFaceAttributes: new[] {
									  	FaceAttributeType.Age, FaceAttributeType.Gender,
										FaceAttributeType.HeadPose, FaceAttributeType.Smile,
										FaceAttributeType.FacialHair, FaceAttributeType.Glasses,
										FaceAttributeType.Emotion, FaceAttributeType.Hair,
										FaceAttributeType.Makeup, FaceAttributeType.Occlusion,
										FaceAttributeType.Accessories, FaceAttributeType.Blur,
										FaceAttributeType.Exposure, FaceAttributeType.Noise
									  }).Result;
		faces.Dump();
	}
}

// Define other methods and classes here
