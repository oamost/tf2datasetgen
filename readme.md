### purpose

This utility program prepares a ready to use data set for Piper neural text to speech system in a fast & convinient way. \
More can be read here: https://github.com/rhasspy/piper/blob/master/TRAINING.md
\
\
The goal is to train AI tts voice model for each TF2 player class from scratch. \
For this, https://wiki.teamfortress.com (mediawiki API) is used as a resource to retreive and organize 
the following data points:

- voice line&emsp;       (wav file)
- subtitle&emsp;&emsp;   (plain text)

The output is a `.csv` file for each player class in a separate output directory,
thus a `wav` directory that contains all the audio for the given player class.
\
\
Integrity check is in place, so the csv file records and the corresponding audio files must remain consistent.

### build

**`$ pacman --query | grep dotnet -i`** 

dotnet-host 8.0.5.sdk105-1 \
dotnet-runtime-bin 8.0.5.sdk300-1 \
dotnet-sdk-bin 8.0.5.sdk300-1 \
dotnet-targeting-pack-bin 8.0.5.sdk300-1 \
\
**`$ dotnet build`**

### output

**`$ tree -L 3 dataset | head -n 10`** 

dataset \
├── demoman \
│   ├── metadata.csv \
│   └── wav \
│       &emsp;├── Demoman_activatecharge01.wav \
│       &emsp;├── Demoman_activatecharge02.wav \
│       &emsp;├── Demoman_activatecharge03.wav \
│       &emsp;├── Demoman_autocappedcontrolpoint01.wav \
│       &emsp;├── Demoman_autocappedcontrolpoint02.wav \
│       &emsp;├── Demoman_autocappedcontrolpoint03.wav 

### limitations

Currently there are almost 6 000 subscripts available on the TF2 media wiki. A few limitations worth to mention: 

1. Some of them don't have a corresponding audio file. These are dropped away from further processing.
2. A few voice lines aren't real speech - like the battalion's backup sound - so they won't get picked into the result set either.
3. Only player classes are processed right now. Mrs. Pauling and the Administrator may be included in a future version.




