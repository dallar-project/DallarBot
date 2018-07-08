using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext.Converters;
using DallarBot.Services;
using DallarBot.Classes;

namespace DallarBot.Commands
{
    // Taken from deep within DSharpPlus so that we can modify its existing behavior slightly
    [RequireBotPermissions(DSharpPlus.Permissions.ManageMessages)]
    public class HelpCommands : BaseCommandModule
    {
        [Command("help"), Description("Displays command help.")]
        public async Task DefaultHelpAsync(CommandContext ctx, [Description("Optional command to provide help for.")] params string[] command)
        {
            // We have to use reflection because TopLevelCommands is marked private and we're not forking DSharpPlus
            PropertyInfo TopLevelCommandsProp = typeof(CommandsNextExtension).GetProperty("TopLevelCommands", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TopLevelCommandsGetter = TopLevelCommandsProp.GetGetMethod(nonPublic: true);
            var toplevel = ((Dictionary<string, Command>)TopLevelCommandsGetter.Invoke(ctx.CommandsNext, null)).Values.Distinct();

            // We instance our help formatter directly because we don't have access to the help formatting factory
            var helpbuilder = new HelpFormatter(ctx);

            if (command != null && command.Any())
            {
                Command cmd = null;
                var search_in = toplevel;
                foreach (var c in command)
                {
                    if (search_in == null)
                    {
                        cmd = null;
                        break;
                    }

                    // We don't have access to config so fuck it, case insensitive help
                    //if (ctx.Config.CaseSensitive)
                    //    cmd = search_in.FirstOrDefault(xc => xc.Name == c || (xc.Aliases != null && xc.Aliases.Contains(c)));
                    //else
                        cmd = search_in.FirstOrDefault(xc => xc.Name.ToLowerInvariant() == c.ToLowerInvariant() || (xc.Aliases != null && xc.Aliases.Select(xs => xs.ToLowerInvariant()).Contains(c.ToLowerInvariant())));

                    if (cmd == null)
                        break;

                    var cfl = await cmd.RunChecksAsync(ctx, true).ConfigureAwait(false);
                    if (cfl.Any())
                        throw new ChecksFailedException(cmd, ctx, cfl);

                    if (cmd is CommandGroup)
                        search_in = (cmd as CommandGroup).Children;
                    else
                        search_in = null;
                }

                if (cmd == null)
                    throw new CommandNotFoundException(string.Join(" ", command));

                helpbuilder.WithCommand(cmd);

                if (cmd is CommandGroup gx)
                {
                    var sxs = gx.Children.Where(xc => !xc.IsHidden);
                    var scs = new List<Command>();
                    foreach (var sc in sxs)
                    {
                        if (sc.ExecutionChecks == null || !sc.ExecutionChecks.Any())
                        {
                            scs.Add(sc);
                            continue;
                        }

                        var cfl = await sc.RunChecksAsync(ctx, true).ConfigureAwait(false);
                        if (!cfl.Any())
                            scs.Add(sc);
                    }

                    if (scs.Any())
                        helpbuilder.WithSubcommands(scs.OrderBy(xc => xc.Name));
                }
            }
            else
            {
                var sxs = toplevel.Where(xc => !xc.IsHidden);
                var scs = new List<Command>();
                foreach (var sc in sxs)
                {
                    if (sc.ExecutionChecks == null || !sc.ExecutionChecks.Any())
                    {
                        scs.Add(sc);
                        continue;
                    }

                    var cfl = await sc.RunChecksAsync(ctx, true).ConfigureAwait(false);
                    if (!cfl.Any())
                        scs.Add(sc);
                }

                if (scs.Any())
                    helpbuilder.WithSubcommands(scs.OrderBy(xc => xc.Name));
            }

            var hmsg = helpbuilder.Build();

            // The main reason for this change, allowing help to be DM'd and the original command deleted.
            DiscordHelpers.DeleteNonPrivateMessage(ctx);
            await DiscordHelpers.RespondAsDM(ctx, hmsg.Embed).ConfigureAwait(false);
        }
    }
}
