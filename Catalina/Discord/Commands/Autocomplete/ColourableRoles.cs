﻿using Catalina.Database;
using Catalina.Discord.Commands.Preconditions;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FuzzySharp;

namespace Catalina.Discord.Commands.Autocomplete
{
    public class ColourableRoles : AutocompleteHandler
    {

        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter,
            IServiceProvider services
        )
        {
            await using var database = new DatabaseContextFactory().CreateDbContext();

            try
            {
                var value = autocompleteInteraction.Data.Current.Value as string;

                var results = new List<AutocompleteResult>();

                var preliminaryGuildRoleResults = database.GuildProperties.Include(g => g.Roles).AsNoTracking().Where(g => g.ID == context.Guild.Id).SelectMany(g => g.Roles).Where(r => r.IsColourable).Select(r => r.ID).ToList();

                var preliminaryUserRoleResults = (context.User as IGuildUser).RoleIds;

                results = preliminaryGuildRoleResults.Intersect(preliminaryUserRoleResults).Select(r => new AutocompleteResult {
                    Name = context.Guild.GetRole(r).Name,
                    Value = r.ToString()
                }).ToList();

                if (string.IsNullOrEmpty(value))
                    return AutocompletionResult.FromSuccess(results.Take(25));

                var names = results.Select(r => r.Name).ToList();

                var searchResults = Process.ExtractTop(query: value, choices: names, limit: 25, cutoff: 0);

                if (searchResults.Any())
                {
                    var cutResults = searchResults.Where(s => s.Score >= searchResults.First().Score / 2).Select(e => e.Value).ToList();

                    var matches = new List<AutocompleteResult>();

                    foreach (var result in cutResults)
                    {
                        matches.Add(results.FirstOrDefault(z => z.Name == result));
                    }

                    var matchCollection = matches.Count > 25 ? matches.Take(25) : matches;

                    return AutocompletionResult.FromSuccess(matchCollection);
                }
                else
                {
                    return AutocompletionResult.FromError(InteractionCommandError.Unsuccessful, "Couldn't find any results");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);

                return AutocompletionResult.FromError(ex);
            }
        }

        protected override string GetLogString(IInteractionContext context) => $"Getting roles for {context.User}";
    }
    public enum RoleTarget : byte
    {
        Colourable,
        Renameable,
    }
}
