﻿using Catalina.Database;
using Catalina.Discord.Commands.Autocomplete;
using Catalina.Discord.Commands.Preconditions;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Catalina.Discord.Commands.Modules
{
    [RequirePrivilege(AccessLevel.User)]
    [Group("style", "User stylisation")]
    public class StylisationModule : InteractionModuleBase
    {
        [SlashCommand("colour", "Change role colour")]
        public async Task ConfigureRole(
            [Summary("Role")] [Autocomplete(typeof(ColourableRoles))] string roleID, 
            [Summary("Colour")] Color roleColor)
        {
            if (!ulong.TryParse(roleID, out ulong id))
            {
                await Context.Interaction.RespondAsync(embed: new Utils.ErrorMessage (user: Context.User) { Exception = new ArgumentException() }, ephemeral: true);
                return;
            }

            using var database = new DatabaseContextFactory().CreateDbContext();
            //var guildProperties = database.GuildProperties.Include(g => g.Roles).AsNoTracking().FirstOrDefault(g => g.ID == Context.Guild.Id);
            var guildProperties = database.GuildProperties.AsNoTracking().FirstOrDefault(g => g.ID == Context.Guild.Id);
            Database.Models.Role DBrole;
            
            DBrole = guildProperties.Roles.FirstOrDefault(r => r.ID == id);

            if (DBrole == null)
            {
                DBrole = new Database.Models.Role { ID = id };
                guildProperties.Roles.Add(DBrole);
                database.SaveChanges();
            }

            try
            {
                var role = Context.Guild.GetRole(ulong.Parse(roleID));

                //var preliminaryGuildRoleResults = database.GuildProperties.Include(g => g.Roles).AsNoTracking().SelectMany(g => g.Roles).Where(r => r.IsColourable).Select(r => r.ID).ToList();
                var preliminaryGuildRoleResults = database.GuildProperties.AsNoTracking().SelectMany(g => g.Roles).Where(r => r.IsColourable).Select(r => r.ID).ToList();
                var preliminaryUserRoleResults = (Context.User as IGuildUser).RoleIds;

                var results = preliminaryGuildRoleResults.Intersect(preliminaryUserRoleResults);
 

                if (results.Contains(id)) {
                    await role.ModifyAsync(r => r.Color = roleColor);
                    await RespondAsync(embed: new Utils.AcknowledgementMessage (user: Context.User) { Body = $"Changed role colour to {System.Drawing.ColorTranslator.ToHtml(roleColor)}.", Color = roleColor });
                }
                else
                {
                    await Context.Interaction.RespondAsync(embed: new Utils.ErrorMessage (user: Context.User){ Exception = new UnauthorizedAccessException() }, ephemeral: true);
                }
            }
            catch
            {
                await Context.Interaction.RespondAsync(embed: new Utils.ErrorMessage (user: Context.User){ Exception = new ArgumentException() }, ephemeral: true);
                return;

            }
        }

        [SlashCommand("name", "Change role name")]
        public async Task ConfigureRole(
            [Summary("Role")] [Autocomplete(typeof(ColourableRoles))] string roleID,
            [Summary("Name")] string roleName)
        {
            if (!ulong.TryParse(roleID, out ulong id))
            {
                await Context.Interaction.RespondAsync(embed: new Utils.ErrorMessage (user: Context.User) { Exception = new ArgumentException() }, ephemeral: true);
                return;
            }

            using var database = new DatabaseContextFactory().CreateDbContext();
            //var guildProperties = database.GuildProperties.Include(g => g.Roles).AsNoTracking().FirstOrDefault(g => g.ID == Context.Guild.Id);
            var guildProperties = database.GuildProperties.AsNoTracking().FirstOrDefault(g => g.ID == Context.Guild.Id);
            Database.Models.Role DBrole;

            DBrole = guildProperties.Roles.FirstOrDefault(r => r.ID == id);

            if (DBrole == null)
            {
                DBrole = new Database.Models.Role { ID = id };
                guildProperties.Roles.Add(DBrole);
                database.SaveChanges();
            }

            try
            {
                var role = Context.Guild.GetRole(ulong.Parse(roleID));

                //var preliminaryGuildRoleResults = database.GuildProperties.Include(g => g.Roles).AsNoTracking().SelectMany(g => g.Roles).Where(r => r.IsColourable).Select(r => r.ID).ToList();
                var preliminaryGuildRoleResults = database.GuildProperties.SelectMany(g => g.Roles).Where(r => r.IsColourable).Select(r => r.ID).ToList();
                var preliminaryUserRoleResults = (Context.User as IGuildUser).RoleIds;

                var results = preliminaryGuildRoleResults.Intersect(preliminaryUserRoleResults);


                if (results.Contains(id))
                {
                    await role.ModifyAsync(r => r.Name = roleName);
                    await RespondAsync(embed: new Utils.AcknowledgementMessage (user: Context.User) { Body = $"Changed role name to {roleName}.", Color = CatalinaColours.Green });
                }
                else
                {
                    await Context.Interaction.RespondAsync(embed: new Utils.ErrorMessage (user: Context.User) { Exception = new UnauthorizedAccessException() }, ephemeral: true);
                }

            }
            catch
            {
                await Context.Interaction.RespondAsync(embed: new Utils.ErrorMessage (user: Context.User) { Exception = new ArgumentException() }, ephemeral: true);
                return;
            }
        }

    }
}
