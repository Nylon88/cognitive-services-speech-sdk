//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Carbon;

namespace Carbon.Recognition.Speech
{
    /// <summary>
    /// Performs speech recogniztion from microphone, file, or other audio input streams, and gets transcribed text as result.
    /// </summary>
    /// <example>
    /// An example to use the speech recognizer on a audio file and listen to events generated by the recognizer.
    /// <code>
    /// static void MySessionEventHandler(object sender, SessionEventArgs e)
    /// {
    ///    Console.WriteLine(String.Format("Speech recogniton: Session event: {0} ", e.ToString()));
    /// }
    ///
    /// static void MyIntermediateResultEventHandler(object sender, SpeechRecognitionResultEventArgs e)
    /// {
    ///    Console.WriteLine(String.Format("Speech recogniton: Intermediate result: {0} ", e.ToString()));
    /// }
    ///
    /// static void MyFinalResultEventHandler(object sender, SpeechRecognitionResultEventArgs e)
    /// {
    ///    Console.WriteLine(String.Format("Speech recogniton: Final result: {0} ", e.ToString()));
    /// }
    ///
    /// static void MyErrorHandler(object sender, RecognitionErrorEventArgs e)
    /// {
    ///    Console.WriteLine(String.Format("Speech recogniton: Error information: {0} ", e.ToString()));
    /// }
    ///
    /// static void SpeechRecognizerSample()
    /// {
    ///   SpeechRecognizer reco = factory.CreateSpeechRecognizer("audioFileName");
    ///
    ///   reco.OnSessionEvent += MySessionEventHandler;
    ///   reco.FinalResultReceived += MyFinalResultEventHandler;
    ///   reco.IntermediateResultReceived += MyIntermediateResultEventHandler;
    ///   reco.RecognitionErrorRaised += MyErrorHandler;
    ///
    ///   // Starts recognition.
    ///   var result = await reco.RecognizeAsync();
    ///
    ///   reco.OnSessionEvent -= MySessionEventHandler;
    ///   reco.FinalResultReceived -= MyFinalResultEventHandler;
    ///   reco.IntermediateResultReceived -= MyIntermediateResultEventHandler;
    ///   reco.RecognitionErrorRaised -= MyErrorHandler;
    ///
    ///   Console.WriteLine("Speech Recognition: Recognition result: " + result);
    /// }
    /// </code>
    /// </example>
    public sealed class SpeechRecognizer : Recognition.Recognizer
    {
        /// <summary>
        /// The event <see cref="IntermediateResultReceived"/> signals that an intermediate recongition result is received.
        /// </summary>
        public event EventHandler<SpeechRecognitionResultEventArgs> IntermediateResultReceived;

        /// <summary>
        /// The event <see cref="FinalResultReceived"/> signals that a final recognition result is received.
        /// </summary>
        public event EventHandler<SpeechRecognitionResultEventArgs> FinalResultReceived;

        /// <summary>
        /// The event <see cref="RecognitionErrorRaised"/> signals that an error occured during recognition.
        /// </summary>
        public event EventHandler<RecognitionErrorEventArgs> RecognitionErrorRaised;

        internal SpeechRecognizer(Internal.SpeechRecognizer recoImpl)
        {
            this.recoImpl = recoImpl;

            intermediateResultHandler = new ResultHandlerImpl(this, isFinalResultHandler: false);
            recoImpl.IntermediateResult.Connect(intermediateResultHandler);

            finalResultHandler = new ResultHandlerImpl(this, isFinalResultHandler: true);
            recoImpl.FinalResult.Connect(finalResultHandler);

            errorHandler = new ErrorHandlerImpl(this);
            recoImpl.NoMatch.Connect(errorHandler);
            recoImpl.Canceled.Connect(errorHandler);

            recoImpl.SessionStarted.Connect(sessionStartedHandler);
            recoImpl.SessionStopped.Connect(sessionStoppedHandler);
            recoImpl.SpeechStartDetected.Connect(speechStartDetectedHandler);
            recoImpl.SpeechEndDetected.Connect(speechEndDetectedHandler);

            Parameters = new ParameterCollection<SpeechRecognizer>(this);
        }

        /// <summary>
        /// Gets/gets the deployment id of a customized speech model that is used for speech recognition.
        /// </summary>
        public string DeploymentId
        {
            get
            {
                return Parameters.Get<string>(ParameterNames.SpeechModelId);
            }

            set
            {
                Parameters.Set(ParameterNames.SpeechModelId, value);
            }
        }

        /// <summary>
        /// Gets/sets the spoken language of recognition.
        /// </summary>
        public string Language
        {
            get
            {
                return Parameters.Get<string>(ParameterNames.SpeechRecognitionLanguage);
            }

            set
            {
                Parameters.Set(ParameterNames.SpeechRecognitionLanguage, value);
            }
        }

        /// <summary>
        /// The collection of parameters and their values defined for this <see cref="SpeechRecognizer"/>.
        /// </summary>
        public ParameterCollection<SpeechRecognizer> Parameters { get; }

        /// <summary>
        /// Starts speech recognition, and stops after the first utterance is recognized. The task returns the recognition text as result.
        /// </summary>
        /// <returns>A task representing the recognition operation. The task returns a value of <see cref="SpeechRecognitionResult"/> </returns>
        /// <example>
        /// The following example creates a speech recognizer, and then gets and prints the recognition result.
        /// <code>
        /// static void SpeechRecognizerSample()
        /// {
        ///   SpeechRecognizer reco = factory.CreateSpeechRecognizer("audioFileName");
        ///
        ///   // Starts recognition.
        ///   var result = await reco.RecognizeAsync();
        ///
        ///   Console.WriteLine("Speech Recognition: Recognition result: " + result);
        /// }
        /// </code>
        /// </example>
        public Task<SpeechRecognitionResult> RecognizeAsync()
        {
            return Task.Run(() => { return new SpeechRecognitionResult(this.recoImpl.Recognize()); });
        }

        /// <summary>
        /// Starts speech recognition on a continous audio stream, until StopContinuousRecognitionAsync() is called.
        /// User must subscribe to events to receive recognition results.
        /// </summary>
        /// <returns>A task representing the asynchronous operation that starts the recognition.</returns>
        public Task StartContinuousRecognitionAsync()
        {
            return Task.Run(() => { this.recoImpl.StartContinuousRecognition(); });
        }

        /// <summary>
        /// Stops continuous speech recognition.
        /// </summary>
        /// <returns>A task representing the asynchronous operation that stops the recognition.</returns>
        public Task StopContinuousRecognitionAsync()
        {
            return Task.Run(() => { this.recoImpl.StopContinuousRecognition(); });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                recoImpl.IntermediateResult.Disconnect(intermediateResultHandler);
                recoImpl.FinalResult.Disconnect(finalResultHandler);
                recoImpl.NoMatch.Disconnect(errorHandler);
                recoImpl.Canceled.Disconnect(errorHandler);
                recoImpl.SessionStarted.Disconnect(sessionStartedHandler);
                recoImpl.SessionStopped.Disconnect(sessionStoppedHandler);
                recoImpl.SpeechStartDetected.Disconnect(speechStartDetectedHandler);
                recoImpl.SpeechEndDetected.Disconnect(speechEndDetectedHandler);

                intermediateResultHandler.Dispose();
                finalResultHandler.Dispose();
                errorHandler.Dispose();
                recoImpl.Dispose();
                Parameters.Dispose();
                disposed = true;
                base.Dispose(disposing);
            }
        }

        internal Internal.SpeechRecognizer recoImpl;
        private ResultHandlerImpl intermediateResultHandler;
        private ResultHandlerImpl finalResultHandler;
        private ErrorHandlerImpl errorHandler;
        private bool disposed = false;

        // Defines an internal class to raise a C# event for intermediate/final result when a corresponding callback is invoked by the native layer.
        private class ResultHandlerImpl : Internal.SpeechRecognitionEventListener
        {
            public ResultHandlerImpl(SpeechRecognizer recognizer, bool isFinalResultHandler)
            {
                this.recognizer = recognizer;
                this.isFinalResultHandler = isFinalResultHandler;
            }

            public override void Execute(Internal.SpeechRecognitionEventArgs eventArgs)
            {
                if (recognizer.disposed)
                {
                    return;
                }

                var resultEventArg = new SpeechRecognitionResultEventArgs(eventArgs);
                var handler = isFinalResultHandler ? recognizer.FinalResultReceived : recognizer.IntermediateResultReceived;
                if (handler != null)
                {
                    handler(this.recognizer, resultEventArg);
                }
            }

            private SpeechRecognizer recognizer;
            private bool isFinalResultHandler;
        }

        // Defines an internal class to raise a C# event for error during recognition when a corresponding callback is invoked by the native layer.
        private class ErrorHandlerImpl : Internal.SpeechRecognitionEventListener
        {
            public ErrorHandlerImpl(SpeechRecognizer recognizer)
            {
                this.recognizer = recognizer;
            }

            public override void Execute(Carbon.Internal.SpeechRecognitionEventArgs eventArgs)
            {
                if (recognizer.disposed)
                {
                    return;
                }

                var resultEventArg = new RecognitionErrorEventArgs(eventArgs.SessionId, eventArgs.Result.Reason);
                var handler = this.recognizer.RecognitionErrorRaised;

                if (handler != null)
                {
                    handler(this.recognizer, resultEventArg);
                }
            }

            private SpeechRecognizer recognizer;
        }
    }

}
