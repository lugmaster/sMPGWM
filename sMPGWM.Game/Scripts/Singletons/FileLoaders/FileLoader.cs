using System;
using Godot;
using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.Singletons.Autoload;
using sMPGWM.Scripts.Singletons.Base;

namespace sMPGWM.Scripts.Singletons.FileLoaders;

public partial class FileLoader : AbstractSingleton<FileLoader>
{
    
    protected static LoadResult<JToken> LoadJsonAndValidate(
        string userPath,
        string defaultPath,
        string schemaPath)
    {
        var filesResult = PrepareAndReadFiles(
            userPath, defaultPath, schemaPath, "JSON", "JSON schema");

        if (!filesResult.IsSuccess)
            return LoadResult<JToken>.Failure(filesResult.Error!);

        var files = filesResult.Value!;
        return JsonParser.ValidateAndReturn(
            files.DefinitionText, files.SchemaText, userPath, schemaPath);
    }

    protected static LoadResult<TDefinition> LoadJsonDefinition<TDefinition>(
        string userPath,
        string defaultPath,
        string schemaPath)
        where TDefinition : class
    {
        var filesResult = PrepareAndReadFiles(
            userPath, defaultPath, schemaPath, "JSON", "JSON schema");

        if (!filesResult.IsSuccess)
            return LoadResult<TDefinition>.Failure(filesResult.Error!);

        var files = filesResult.Value!;
        return JsonParser.ValidateAndDeserialize<TDefinition>(
            files.DefinitionText, files.SchemaText, userPath, schemaPath);
    }

    protected static LoadResult<JToken> LoadYamlAndValidate(
        string userPath,
        string defaultPath,
        string schemaPath)
    {
        var filesResult = PrepareAndReadFiles(
            userPath, defaultPath, schemaPath, "YAML", "YAML schema");

        if (!filesResult.IsSuccess)
            return LoadResult<JToken>.Failure(filesResult.Error!);

        var files = filesResult.Value!;
        return YamlParser.ValidateAndReturn(
            files.DefinitionText, files.SchemaText, userPath, schemaPath);
    }

    protected static LoadResult<TDefinition> LoadYamlDefinition<TDefinition>(
        string userPath,
        string defaultPath,
        string schemaPath)
        where TDefinition : class
    {
        var filesResult = PrepareAndReadFiles(
            userPath, defaultPath, schemaPath, "YAML", "YAML schema");

        if (!filesResult.IsSuccess)
            return LoadResult<TDefinition>.Failure(filesResult.Error!);

        var files = filesResult.Value!;
        return YamlParser.ValidateAndDeserialize<TDefinition>(
            files.DefinitionText, files.SchemaText, userPath, schemaPath);
    }

    protected static void AbortLoad(DataLoadError error)
    {
        var message = error.ToDisplayString();
        Logger.Info(message);
        GameHandler.Instance.QuitToStartMenuBlocking(message);
    }

    private static LoadResult<DefinitionFileContents> PrepareAndReadFiles(
        string userPath,
        string defaultPath,
        string schemaPath,
        string dataFormat,
        string schemaFormat)
    {
        // TODO replace with proper config!
        var reloadConfig = true;

        if (reloadConfig || !FileAccess.FileExists(userPath))
        {
            Logger.Info(
                $"{(reloadConfig ? "Forced reload" : "Missing file")} — " +
                $"copying default from {defaultPath} to {userPath}");

            var copyResult = CopyDefaultFile(defaultPath, userPath, dataFormat);
            if (!copyResult.IsSuccess)
                return LoadResult<DefinitionFileContents>.Failure(copyResult.Error!);
        }

        var definitionResult = ReadTextFile(userPath, $"{dataFormat} definition file");
        if (!definitionResult.IsSuccess)
            return LoadResult<DefinitionFileContents>.Failure(definitionResult.Error!);

        var schemaResult = ReadTextFile(schemaPath, $"{schemaFormat} file");
        if (!schemaResult.IsSuccess)
            return LoadResult<DefinitionFileContents>.Failure(schemaResult.Error!);

        return LoadResult<DefinitionFileContents>.Success(
            new DefinitionFileContents(definitionResult.Value!, schemaResult.Value!));
    }

    private static LoadResult<bool> CopyDefaultFile(
        string defaultPath,
        string userPath,
        string dataFormat)
    {
        if (!FileAccess.FileExists(defaultPath))
        {
            return LoadResult<bool>.Failure(new DataLoadError(
                "Default file not found",
                $"The packaged default {dataFormat} file does not exist.",
                defaultPath));
        }

        var userDirectory = userPath.GetBaseDir();
        var directoryError = DirAccess.MakeDirRecursiveAbsolute(userDirectory);
        if (directoryError is not Error.Ok and not Error.AlreadyExists)
        {
            return LoadResult<bool>.Failure(new DataLoadError(
                "Directory creation failed",
                $"Godot error: {directoryError}",
                userDirectory));
        }

        using var sourceFile = FileAccess.Open(defaultPath, FileAccess.ModeFlags.Read);
        if (sourceFile is null)
        {
            return LoadResult<bool>.Failure(new DataLoadError(
                "Default file could not be opened",
                $"Godot error: {FileAccess.GetOpenError()}",
                defaultPath));
        }

        var content = sourceFile.GetAsText();
        var sourceReadError = sourceFile.GetError();
        if (sourceReadError != Error.Ok)
        {
            return LoadResult<bool>.Failure(new DataLoadError(
                "Default file could not be read",
                $"Godot error: {sourceReadError}",
                defaultPath));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return LoadResult<bool>.Failure(new DataLoadError(
                "Default file is empty",
                $"The packaged default {dataFormat} file contains no usable data.",
                defaultPath));
        }

        using var targetFile = FileAccess.Open(userPath, FileAccess.ModeFlags.Write);
        if (targetFile is null)
        {
            return LoadResult<bool>.Failure(new DataLoadError(
                "User file could not be created",
                $"Godot error: {FileAccess.GetOpenError()}",
                userPath));
        }

        targetFile.StoreString(content);
        targetFile.Flush();

        var writeError = targetFile.GetError();
        if (writeError != Error.Ok)
        {
            return LoadResult<bool>.Failure(new DataLoadError(
                "User file could not be written",
                $"Godot error: {writeError}",
                userPath));
        }

        if (!FileAccess.FileExists(userPath))
        {
            return LoadResult<bool>.Failure(new DataLoadError(
                "User file missing after copy",
                "The write operation reported no error, but the destination file does not exist.",
                userPath));
        }

        Logger.Info($"Copied default {dataFormat} file to {userPath}");
        return LoadResult<bool>.Success(true);
    }

    private static LoadResult<string> ReadTextFile(string path, string description)
    {
        if (!FileAccess.FileExists(path))
        {
            return LoadResult<string>.Failure(new DataLoadError(
                $"{description} not found",
                "The requested file does not exist.",
                path));
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file is null)
        {
            return LoadResult<string>.Failure(new DataLoadError(
                $"{description} could not be opened",
                $"Godot error: {FileAccess.GetOpenError()}",
                path));
        }

        var content = file.GetAsText();
        var readError = file.GetError();
        if (readError != Error.Ok)
        {
            return LoadResult<string>.Failure(new DataLoadError(
                $"{description} could not be read",
                $"Godot error: {readError}",
                path));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return LoadResult<string>.Failure(new DataLoadError(
                $"{description} is empty",
                "The file contains no usable content.",
                path));
        }

        return LoadResult<string>.Success(content);
    }

    private sealed record DefinitionFileContents(
        string DefinitionText,
        string SchemaText);
}