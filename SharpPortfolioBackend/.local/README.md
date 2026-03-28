# SharpPortfolioBackend

## Testing Audio Uploads

To test the audio upload endpoint using the provided `audio.http` file:

1. Open `.local/audio.http`.
2. Ensure you have a `.wav` file on your machine.
3. Update the `@LocalWavFilePath` variable at the top of the file with the absolute path to your `.wav` file.
   - Example: `@LocalWavFilePath = C:\Users\YourUser\Downloads\test.wav`
4. Run the "Create Audio" request.

### How it works
The HTTP Client uses `multipart/form-data` to send the file and its metadata.
The `<` symbol in the request body is a special operator that tells the client to read the contents of the specified file and include it in that part of the form.

```http
--WebAppBoundary
Content-Disposition: form-data; name="File"; filename="test.wav"
Content-Type: audio/wav

< {{LocalWavFilePath}}
```

Note: The `Vibes` field in `CreateAudioDto` is a list. To send multiple vibes, include multiple `Vibes` form-data parts in the request.
