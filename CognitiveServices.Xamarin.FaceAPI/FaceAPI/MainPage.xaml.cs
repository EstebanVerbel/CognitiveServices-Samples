﻿using FaceAPI.Models;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Plugin.Connectivity;
using Plugin.Media;
using Plugin.Media.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace FaceAPI
{
    public partial class MainPage : ContentPage
    {
        private readonly IFaceServiceClient faceServiceClient;

        public MainPage()
        {
            InitializeComponent();

            this.faceServiceClient = new FaceServiceClient("496b22058ec24307bbd09b0654f5fa03");
        }

        private async void UploadPictureButton_Clicked(object sender, EventArgs e)
        {
            if (!CrossMedia.Current.IsPickPhotoSupported)
            {
                await DisplayAlert("No upload", "Picking a photo is not supported.", "OK");
                return;
            }

            var file = await CrossMedia.Current.PickPhotoAsync();
            if (file == null)
            {
                return;
            }

            this.Indicator1.IsVisible = true;
            this.Indicator1.IsRunning = true;
            Image1.Source = ImageSource.FromStream(() => file.GetStream());

            await DetectFacesAsync(file);

            this.Indicator1.IsRunning = false;
            this.Indicator1.IsVisible = false;
        }

        private async void TakePictureButton_Clicked(object sender, EventArgs e)
        {
            await CrossMedia.Current.Initialize();

            if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
            {
                await DisplayAlert("No Camera", "No camera available.", "OK");
                return;
            }

            var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
            {
                SaveToAlbum = true,
                Name = "test.jpg"
            });

            if (file == null)
            {
                return;
            }

            this.Indicator1.IsVisible = true;
            this.Indicator1.IsRunning = true;
            Image1.Source = ImageSource.FromStream(() => file.GetStream());

            await DetectFacesAsync(file);

            this.Indicator1.IsRunning = false;
            this.Indicator1.IsVisible = false;
        }

        private async Task<FaceDetection> DetectFacesAsync(MediaFile inputFile)
        {
            if (!CrossConnectivity.Current.IsConnected)
            {
                await DisplayAlert("Network error", "Please check your network connection and retry.", "OK");
                return null;
            }

            // The list of Face attributes to return.
            IEnumerable<FaceAttributeType> faceAttributes = new FaceAttributeType[]
            {
                FaceAttributeType.Gender,
                FaceAttributeType.Age,
                FaceAttributeType.Smile,
                FaceAttributeType.Emotion,
                FaceAttributeType.Glasses,
                FaceAttributeType.FacialHair,
                FaceAttributeType.HeadPose,
            };

            // Call the Face API.
            try
            {

                Face[] faces = await faceServiceClient.DetectAsync(
                    inputFile.GetStream(),
                    returnFaceId: true,
                    returnFaceLandmarks: false,
                    returnFaceAttributes: faceAttributes);

                //var faceAttributes = faces[0]?.FaceAttributes;
                if (faces.Length == 0)
                    return null;

                var emotion = faces[0].FaceAttributes.Emotion.ToRankedList();
                FaceDetection theData = new FaceDetection()
                { 
                    Age = faces[0].FaceAttributes.Age,
                    Beard = faces[0].FaceAttributes.FacialHair.Beard,
                    Emotion = emotion.FirstOrDefault().Key,
                    Gender = faces[0].FaceAttributes.Gender,
                    Glasses = faces[0].FaceAttributes.Glasses.ToString(),
                    Moustache = faces[0].FaceAttributes.FacialHair.Moustache,
                    Smile = faces[0].FaceAttributes.Smile
                };

                this.BindingContext = theData;
                this.Indicator1.IsRunning = false;
                this.Indicator1.IsVisible = false;

                return theData;
            }
            catch (FaceAPIException f)
            {
                await DisplayAlert("Network error", f.ErrorMessage, "OK");
                return null;
            }
            catch (Exception e)
            {
                await DisplayAlert("Error", e.Message, "OK");
                return null;
            }  
        }
    }
}
