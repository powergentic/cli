using System.CommandLine;
using Pga.Cli.Rendering;
using Pga.Core.Configuration;
using Spectre.Console;

namespace Pga.Cli.Commands;

/// <summary>
/// Configuration management commands.
/// </summary>
public static class ConfigCommand
{
    public static Command Create()
    {
        var command = new Command("config", "Manage PGA configuration (LLM profiles, settings).");

        command.Subcommands.Add(CreateInitCommand());
        command.Subcommands.Add(CreateShowCommand());
        command.Subcommands.Add(CreateAddProfileCommand());
        command.Subcommands.Add(CreateRemoveProfileCommand());
        command.Subcommands.Add(CreateListProfilesCommand());
        command.Subcommands.Add(CreateSetDefaultCommand());
        command.Subcommands.Add(CreateValidateCommand());

        return command;
    }

    private static Command CreateInitCommand()
    {
        var command = new Command("init", "Initialize PGA configuration at ~/.powergentic/config.json");

        command.SetAction(parseResult =>
        {
            var manager = new ConfigManager();
            if (manager.Initialize())
            {
                ConsoleRenderer.RenderSuccess($"Configuration created at {ConfigManager.GlobalConfigFilePath}");
                ConsoleRenderer.RenderInfo("Edit this file to add your LLM provider credentials.");
            }
            else
            {
                ConsoleRenderer.RenderInfo($"Configuration already exists at {ConfigManager.GlobalConfigFilePath}");
            }
        });

        return command;
    }

    private static Command CreateShowCommand()
    {
        var command = new Command("show", "Display the current configuration (secrets are masked).");

        command.SetAction(parseResult =>
        {
            var manager = new ConfigManager();
            var config = manager.Load();

            var tree = new Tree("[bold]PGA Configuration[/]");
            tree.Style = new Style(Color.Blue);

            var generalNode = tree.AddNode("[bold]General[/]");
            generalNode.AddNode($"Config path: {manager.ConfigFilePath}");
            generalNode.AddNode($"Default profile: {config.DefaultProfile}");
            generalNode.AddNode($"Tool safety: {config.ToolSafety.Mode}");
            generalNode.AddNode($"Stream responses: {config.Ui.StreamResponses}");
            generalNode.AddNode($"Show tool calls: {config.Ui.ShowToolCalls}");

            var profilesNode = tree.AddNode("[bold]LLM Profiles[/]");
            foreach (var (name, profile) in config.Profiles)
            {
                var isDefault = name == config.DefaultProfile ? " [green](default)[/]" : "";
                var pNode = profilesNode.AddNode($"[bold]{name}[/]{isDefault}");
                pNode.AddNode($"Provider: {profile.Provider}");

                switch (profile.Provider.ToLowerInvariant())
                {
                    case "azure-openai":
                    case "azure-ai-foundry":
                        pNode.AddNode($"Endpoint: {profile.Endpoint ?? "(not set)"}");
                        pNode.AddNode($"Deployment: {profile.DeploymentName ?? "(not set)"}");
                        pNode.AddNode($"Auth mode: {profile.AuthMode}");
                        pNode.AddNode($"API Key: {MaskSecret(profile.ApiKey)}");
                        break;
                    case "ollama":
                        pNode.AddNode($"Host: {profile.OllamaHost}");
                        pNode.AddNode($"Model: {profile.OllamaModel ?? "(not set)"}");
                        break;
                }
            }

            if (config.AutoSelect.Enabled && config.AutoSelect.Rules.Count > 0)
            {
                var autoNode = tree.AddNode("[bold]Auto-Select Rules[/]");
                foreach (var rule in config.AutoSelect.Rules)
                {
                    autoNode.AddNode($"{rule.Pattern} → {rule.Profile} ({rule.Description ?? "no description"})");
                }
            }

            AnsiConsole.Write(tree);
        });

        return command;
    }

    private static Command CreateAddProfileCommand()
    {
        var nameArg = new Argument<string>("name")
        {
            Description = "The name for the new profile."
        };

        var command = new Command("add-profile", "Add a new LLM profile interactively.")
        {
            nameArg
        };

        command.SetAction(parseResult =>
        {
            var name = parseResult.GetValue(nameArg)!;
            var manager = new ConfigManager();

            var provider = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select the LLM provider:")
                    .AddChoices("azure-openai", "azure-ai-foundry", "ollama"));

            var profile = new LlmProfile { Provider = provider };

            switch (provider)
            {
                case "azure-openai":
                case "azure-ai-foundry":
                    profile.Endpoint = AnsiConsole.Ask<string>("Azure OpenAI endpoint URL:");
                    profile.DeploymentName = AnsiConsole.Ask<string>("Deployment name:");

                    profile.AuthMode = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Authentication mode:")
                            .AddChoices("key", "entra"));

                    if (profile.AuthMode == "key")
                        profile.ApiKey = AnsiConsole.Prompt(new TextPrompt<string>("API key:").Secret());
                    else
                        profile.TenantId = AnsiConsole.Ask<string>("Tenant ID (optional, press Enter to skip):", "");

                    break;

                case "ollama":
                    profile.OllamaHost = AnsiConsole.Ask("Ollama host URL:", "http://localhost:11434");
                    profile.OllamaModel = AnsiConsole.Ask<string>("Model name (e.g., llama3, codestral):");
                    break;
            }

            profile.DisplayName = AnsiConsole.Ask("Display name (optional):", name);

            manager.UpsertProfile(name, profile);
            ConsoleRenderer.RenderSuccess($"Profile '{name}' added successfully.");

            // Offer to set as default
            var config = manager.Load();
            if (config.Profiles.Count == 1 || AnsiConsole.Confirm($"Set '{name}' as the default profile?"))
            {
                config.DefaultProfile = name;
                manager.Save(config);
                ConsoleRenderer.RenderSuccess($"'{name}' set as the default profile.");
            }
        });

        return command;
    }

    private static Command CreateRemoveProfileCommand()
    {
        var nameArg = new Argument<string>("name")
        {
            Description = "The name of the profile to remove."
        };

        var command = new Command("remove-profile", "Remove an LLM profile.")
        {
            nameArg
        };

        command.SetAction(parseResult =>
        {
            var name = parseResult.GetValue(nameArg)!;
            var manager = new ConfigManager();
            if (manager.RemoveProfile(name))
                ConsoleRenderer.RenderSuccess($"Profile '{name}' removed.");
            else
                ConsoleRenderer.RenderError($"Profile '{name}' not found.");
        });

        return command;
    }

    private static Command CreateListProfilesCommand()
    {
        var command = new Command("list-profiles", "List all configured LLM profiles.");

        command.SetAction(parseResult =>
        {
            var manager = new ConfigManager();
            var config = manager.Load();

            if (config.Profiles.Count == 0)
            {
                ConsoleRenderer.RenderInfo("No profiles configured. Run 'pga config add-profile <name>' to add one.");
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Name")
                .AddColumn("Provider")
                .AddColumn("Details")
                .AddColumn("Default");

            foreach (var (name, profile) in config.Profiles)
            {
                var details = profile.Provider.ToLowerInvariant() switch
                {
                    "azure-openai" or "azure-ai-foundry" =>
                        $"{profile.DeploymentName} @ {profile.Endpoint}",
                    "ollama" =>
                        $"{profile.OllamaModel} @ {profile.OllamaHost}",
                    _ => profile.Provider
                };

                var isDefault = name == config.DefaultProfile ? "✓" : "";
                table.AddRow(name, profile.Provider, details, isDefault);
            }

            AnsiConsole.Write(table);
        });

        return command;
    }

    private static Command CreateSetDefaultCommand()
    {
        var nameArg = new Argument<string>("name")
        {
            Description = "The profile name to set as default."
        };

        var command = new Command("set-default", "Set the default LLM profile.")
        {
            nameArg
        };

        command.SetAction(parseResult =>
        {
            var name = parseResult.GetValue(nameArg)!;
            var manager = new ConfigManager();
            var config = manager.Load();

            if (!config.Profiles.ContainsKey(name))
            {
                ConsoleRenderer.RenderError($"Profile '{name}' not found.");
                return;
            }

            config.DefaultProfile = name;
            manager.Save(config);
            ConsoleRenderer.RenderSuccess($"Default profile set to '{name}'.");
        });

        return command;
    }

    private static Command CreateValidateCommand()
    {
        var command = new Command("validate", "Validate the current configuration.");

        command.SetAction(parseResult =>
        {
            var manager = new ConfigManager();
            var errors = manager.Validate();

            if (errors.Count == 0)
                ConsoleRenderer.RenderSuccess("Configuration is valid.");
            else
            {
                ConsoleRenderer.RenderError("Configuration has errors:");
                foreach (var error in errors)
                    ConsoleRenderer.RenderError($"  • {error}");
            }
        });

        return command;
    }

    private static string MaskSecret(string? secret)
    {
        if (string.IsNullOrEmpty(secret)) return "(not set)";
        if (secret.Length <= 8) return "****";
        return secret[..4] + "****" + secret[^4..];
    }
}
