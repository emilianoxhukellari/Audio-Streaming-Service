# Audio-Streaming-Service

## Abstract

This project intended to develop an audio streaming service that supports multiple clients simultaneously and provides high-fidelity (HiFi) audio over the network. To achieve this goal, I implemented a low-level audio player that utilizes native Windows multimedia functions. Furthermore, I have implemented a substantial networking protocol that uses the reliable Transmission Control Protocol (TCP). The audio streaming service manages to stream HiFi audio in a deterministic and dynamic manner based on how the users interact with their applications. The result is a two-application service that takes advantage of multithreading for maximum performance for streaming audio and for performing separate tasks. With the help of numerous algorithms, some of which run over the network, our audio streaming service manages to provide users with a satisfactory audio streaming experience.

## Configuring and Running the Applications

Before running the Client Application and the Server Application, you need to configure them by setting the correct IPv4 Addresses for both applications.

### Server Application
First, you need to obtain the IPv4 Address of the computer where you will be running the Server Application. 
1. Open CMD and run “ipconfig”
2. Get the IPv4

Next, we will configure the Release version of both applications. <br/>
For the Server Application go to: Server Application > Server Application > bin > Release > net6.0-windows > Server Application.dll.config <br/>
When you open Server Application.dll.config, you will need to change only one of the attributes [key=“Host”]. Set the value of “Host” to the IPv4 Address you obtained previously [value=”x.x.x.x”]. <br/>
The server is now ready to be executed. In the current directory, you can find “Server Application.exe”. <br/>

### Client Application
For this step, we will use the same IPv4 Address we obtained from the server. This will tell the client where it needs to connect. <br/>
Go to:  Client Application > Client Application > bin > Release > net6.0-windows > Client Application.dll.config. Here we only need to change the attribute [key=”Host”]. Set the value of “Host” to the IPv4 Address of the server [value=”x.x.x.x”]. <br/>
The client is now ready to be executed. In the current directory, you can find “Client Application.exe”. <br/>

#### For more details check the documentation: [Audio Streaming Service Documentation.pdf](https://github.com/emilianoxhukellari/Audio-Streaming-Service/files/10717451/Audio.Streaming.Service.Documentation.pdf).
