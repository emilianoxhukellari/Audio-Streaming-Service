using System;

namespace Client_Application.Client
{
    public enum NetworkRequestType
    {
        SearchSongOrArtist,
        TerminateSongDataReceive,
    }

    /// <summary>
    /// Represents a network request.
    /// </summary>
    public sealed class NetworkRequest
    {
        public object[] Parameters { get; }
        public NetworkRequestType Type { get; }
        public NetworkRequest(params object[] parameters)
        {
            Type = (NetworkRequestType)parameters[0];
            Parameters = new object[parameters.Length - 1];
            Array.Copy(parameters, 1, Parameters, 0, parameters.Length - 1);
        }
    }
}
