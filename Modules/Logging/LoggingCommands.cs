using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Zarnogh.Configuration;
using Zarnogh.Other;
using Zarnogh.Services;

namespace Zarnogh.Modules.Logging
{
    [RequireModuleEnabled( "Logging" )]
    public class LoggingCommands : BaseCommandModule
    {
        private readonly BotConfig _botConfig;
        private readonly ZarnoghState _botState;
        private readonly GuildConfigManager _guildConfigManager;
        private readonly ModuleManager _moduleManager;

        public LoggingCommands( BotConfig botConfig, GuildConfigManager guildConfigManager, ModuleManager moduleManager, ZarnoghState botState )
        {
            _botState = botState;
            _botConfig = botConfig;
            _guildConfigManager = guildConfigManager;
            _moduleManager = moduleManager;
        }

        private static readonly List<(string Key, string DisplayName, Func<LogConfig, bool> Getter, Action<LogConfig> Toggler)> _logEventDefinitions = new()
        {
            ("guildmemberremoved", "Member Removed", cfg => cfg.OnGuildMemberRemoved, cfg => cfg.OnGuildMemberRemoved = !cfg.OnGuildMemberRemoved),
            ("guildmemberadded", "Member Added", cfg => cfg.OnGuildMemberAdded, cfg => cfg.OnGuildMemberAdded = !cfg.OnGuildMemberAdded),
            ("guildbanremoved", "Ban Removed", cfg => cfg.OnGuildBanRemoved, cfg => cfg.OnGuildBanRemoved = !cfg.OnGuildBanRemoved),
            ("guildbanadded", "Ban Added", cfg => cfg.OnGuildBanAdded, cfg => cfg.OnGuildBanAdded = !cfg.OnGuildBanAdded),
            ("guildrolecreated", "Role Created", cfg => cfg.OnGuildRoleCreated, cfg => cfg.OnGuildRoleCreated = !cfg.OnGuildRoleCreated),
            ("guildroleupdated", "Role Updated", cfg => cfg.OnGuildRoleUpdated, cfg => cfg.OnGuildRoleUpdated = !cfg.OnGuildRoleUpdated),
            ("guildroledeleted", "Role Deleted", cfg => cfg.OnGuildRoleDeleted, cfg => cfg.OnGuildRoleDeleted = !cfg.OnGuildRoleDeleted),
            ("messagesbulkdeleted", "Messages Bulk Deleted", cfg => cfg.OnMessagesBulkDeleted, cfg => cfg.OnMessagesBulkDeleted = !cfg.OnMessagesBulkDeleted),
            ("messagedeleted", "Message Deleted", cfg => cfg.OnMessageDeleted, cfg => cfg.OnMessageDeleted = !cfg.OnMessageDeleted),
            ("messageupdated", "Message Updated", cfg => cfg.OnMessageUpdated, cfg => cfg.OnMessageUpdated = !cfg.OnMessageUpdated),
            ("messagecreated", "Message Created", cfg => cfg.OnMessageCreated, cfg => cfg.OnMessageCreated = !cfg.OnMessageCreated),
            ("invitedeleted", "Invite Deleted", cfg => cfg.OnInviteDeleted, cfg => cfg.OnInviteDeleted = !cfg.OnInviteDeleted),
            ("invitecreated", "Invite Created", cfg => cfg.OnInviteCreated, cfg => cfg.OnInviteCreated = !cfg.OnInviteCreated),
            ("channeldeleted", "Channel Deleted", cfg => cfg.OnChannelCreated, cfg => cfg.OnChannelCreated = !cfg.OnChannelCreated),
            ("channelcreated", "Channel Created", cfg => cfg.OnChannelDeleted, cfg => cfg.OnChannelDeleted = !cfg.OnChannelDeleted),
        };

        [Command( "ToggleLogEvents" )]
        [Description( "Configures which server events are logged." )]
        [RequireUserPermissions( DSharpPlus.Permissions.Administrator )]
        public async Task ToggleLogEvents( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();

            var profile = await _guildConfigManager.GetOrCreateGuildConfig(ctx.Guild.Id);

            string selectMenuId = $"log_event_toggle_{ctx.Guild.Id}_{ctx.User.Id}_{DateTime.UtcNow.Ticks}";

            DiscordMessage interactiveMessage;
            try
            {
                var embed = BuildLogSettingsEmbed(ctx.Guild, profile.LoggingConfiguration);
                var selectMenu = BuildLogEventSelectMenu(profile.LoggingConfiguration, selectMenuId);
                var builder = new DiscordMessageBuilder()
                    .WithEmbed(embed)
                    .AddComponents(selectMenu);
                interactiveMessage = await ctx.RespondAsync( builder );
            }
            catch ( Exception )
            {
                await ctx.RespondAsync( "An error occurred while displaying the log settings." );
                return;
            }

            var interactivity = ctx.Client.GetInteractivity();

            while ( true )
            {
                InteractivityResult<ComponentInteractionCreateEventArgs> interactionResult =
                await interactivity.WaitForSelectAsync(interactiveMessage, ctx.User, selectMenuId, TimeSpan.FromMinutes(1));

                if ( interactionResult.TimedOut )
                {
                    try
                    {
                        var timeoutEmbed = new DiscordEmbedBuilder
                        {
                            Title = "Log Settings Timed Out",
                            Description = "The configuration session has timed out due to inactivity.",
                            Color = Constants.ZarnoghPink
                        };
                        var disabledMenu = BuildLogEventSelectMenu(profile.LoggingConfiguration, selectMenuId, isDisabled: true);

                        await interactiveMessage.ModifyAsync( new DiscordMessageBuilder().WithEmbed( timeoutEmbed ) );
                    }
                    catch ( Exception e )
                    {
                        Logger.LogError( e.ToString() );
                    }
                    return;
                }

                ComponentInteractionCreateEventArgs componentArgs = interactionResult.Result;

                await componentArgs.Interaction.CreateResponseAsync( DSharpPlus.InteractionResponseType.UpdateMessage );

                List<string> selectedEventKeys = componentArgs.Interaction.Data.Values.ToList();
                var toggledEventsDisplay = new List<string>();

                foreach ( string eventKey in selectedEventKeys )
                {
                    var definition = _logEventDefinitions.FirstOrDefault(def => def.Key == eventKey);
                    if ( definition.Key != null )
                    {
                        bool oldState = definition.Getter(profile.LoggingConfiguration);
                        definition.Toggler( profile.LoggingConfiguration );
                        bool newState = definition.Getter(profile.LoggingConfiguration);
                        toggledEventsDisplay.Add( definition.DisplayName );
                    }
                }

                await _guildConfigManager.SaveGuildConfigAsync( profile );

                string responseContent = $"Successfully toggled `{string.Join(", ", toggledEventsDisplay)}` Event.";
                var updatedEmbed = BuildLogSettingsEmbed(ctx.Guild, profile.LoggingConfiguration);
                var updatedSelectMenu = BuildLogEventSelectMenu(profile.LoggingConfiguration, selectMenuId);

                var updatedMessageBuilder = new DiscordMessageBuilder()
                        .WithContent(responseContent)
                        .WithEmbed(updatedEmbed)
                        .AddComponents(updatedSelectMenu);

                await componentArgs.Interaction.EditOriginalResponseAsync( new DiscordWebhookBuilder( updatedMessageBuilder ) );
            }
        }

        private DiscordSelectComponent BuildLogEventSelectMenu( LogConfig logConfig, string customId, bool isDisabled = false )
        {
            var options = new List<DiscordSelectComponentOption>();
            foreach ( var def in _logEventDefinitions )
            {
                try
                {
                    bool isEnabled = def.Getter(logConfig);
                    options.Add( new DiscordSelectComponentOption(
                        label: def.DisplayName,
                        value: def.Key,
                        description: isDisabled ? ( isEnabled ? "Enabled" : "Disabled" ) : ( isEnabled ? "Currently Enabled - Click to Disable" : "Currently Disabled - Click to Enable" ),
                        isDefault: false
                    ) );
                }
                catch ( Exception )
                {
                }
            }

            return new DiscordSelectComponent( customId,
                isDisabled ? "Configuration ended" : "Toggle log events...",
                options,
                disabled: isDisabled,
                minOptions: 1,
                maxOptions: Math.Max( 1, options.Count ) );
        }

        private DiscordEmbedBuilder BuildLogSettingsEmbed( DiscordGuild guild, LogConfig logConfig )
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = $"Displaying Log Event Settings for {guild.Name}",
                Color = Constants.ZarnoghPink,
                Description = "Select events below to toggle them for the server."
            };

            foreach ( var def in _logEventDefinitions )
            {
                bool isEnabled = def.Getter(logConfig);
                embed.AddField( def.DisplayName, isEnabled ? "✅ Enabled" : "❌ Disabled", inline: true );
            }

            return embed;
        }
    }
}