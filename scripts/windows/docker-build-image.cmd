:: Packing the app and its dependencies
dotnet publish "<project path>\<project name>.csproj"

:: Building the .NET Core docker image
docker build -f "<dockerfile path>" --build-arg EXE_DIR="<project path>/bin/Debug/netcoreapp2.0/publish" -t "<image name>" "<root path>"

:: Docker login
docker login -u <username> -p <password> <Login server>

:: Push the image to Docker repository
docker push <image name>