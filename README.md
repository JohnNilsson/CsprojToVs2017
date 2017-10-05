# Convert your old project files to the new 2017 format
With the introduction of Visual Studio 2017, Microsoft added some optimizations to how a project file can be set up. However, no tooling was made available that performed this conversion as it was not necessary to do since Visual Studio 2017 would work with the old format too.

This project converts an existing csproj to the new format, shortening the project file and using all the nice new features that are part of Visual Studio 2017.

## What does it fix?
There are a number of things [that VS2017 handles differently](http://www.natemcmaster.com/blog/2017/03/09/vs2015-to-vs2017-upgrade/) that are performed by this tool: 
1. Include files using a wildcard as opposed to specifying every single file 
2. A more succint way of defining project references 
3. A more succint way of handling nuget package references
4. Moving some of the attributes that used to be defined in assemblyinfo into the project file
5. Defining the nuget package definition as part of the project file

## How it works
Using the tool is simple, it is a simple command line utitlity that has a single argument being the project file you would like to convert.

For example
`Project2015To2017.exe "D:\Path\To\My\TestProject.csproj"`
or
` git reset --hard && find . -iname "*.csproj" | xargs -n1 /c/dev/github.com/hvanbakel/CsprojToVs2017/Project2015To2017/bin/Debug/net46/Project2015To2017.exe`

After confirming this is an old style project file, it will start performing the conversion. When it has gathered all the data it needs it first creates a backup of the old project file (suffixed with .old) and then generates a new project file in the new format. 
