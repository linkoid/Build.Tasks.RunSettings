# Run Settings Build Tasks
Provides MSBuild tasks for generating [.runsettings files](https://learn.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file).

Available on [NuGet](https://www.nuget.org/packages/Linkoid.Build.Tasks.RunSettings).

## Usage
Add `Linkoid.Build.Tasks.RunSettings` as a package reference in the project file.
```xml
<ItemGroup>
  <PackageReference Include="Linkoid.Build.Tasks.RunSettings" Version="1.0.0" />
</ItemGroup>
```

### Adding Attributes
Add **elements** for the .runsettings file using `<RunSettingElements>` items.
* The item's `Include=` value specifies the path to the target parent of all the elements.
* The item's metadata specifies the child elements.
```xml
<ItemGroup>
  <RunSettingElements Include="RunConfiguration">
    <ResultsDirectory>.\TestResults</ResultsDirectory>
    <TargetFrameworkVersion>$(TargetFramework)</TargetFrameworkVersion>
  </RunSettingElements>
</ItemGroup>
```

### Adding Attributes
Add **attributes** to an element using `<RunSettingAttributes>` items.
* The item's `Include=` value specifies the path to the target element to add the attributes to.
* The item's metadata specifies the attributes.
```xml
<ItemGroup>
  <RunSettingAttributes Include="LoggerRunSettings/Loggers/Logger" >
    <friendlyName>console</friendlyName>
    <enabled Condition="'$(Configuration)' == 'Debug'">true</enabled>
    <enabled Condition="'$(Configuration)' == 'Release'">false</enabled>
  </RunSettingAttributes>
</ItemGroup>
```

### Generated File
The location and name of the generated .runsettings file is determined by the `<RunSettingsFilePath>` MSBuild property,
and defaults to `.runsettings` in the project directory.

A generated .runsettings file can be edited. 
Elements and attributes found in an existing .runsettings file will be preserved when regenerating the file
or overwritten if it is an element or attribute defined in the project file.

Below is the .runsettings file generated with the items defined in the examples above.
```xml
<?xml version="1.0" encoding="utf-8"?>
<!--This file was automatically generated. Additions are fine, but some changes may be overwritten on build.-->
<RunSettings>
  <RunConfiguration>
    <ResultsDirectory>.\TestResults</ResultsDirectory>
    <TargetFrameworkVersion>net6.0</TargetFrameworkVersion>
  </RunConfiguration>
  <LoggerRunSettings>
    <Loggers>
      <Logger enabled="true" friendlyName="console" />
    </Loggers>
  </LoggerRunSettings>
</RunSettings>
```
