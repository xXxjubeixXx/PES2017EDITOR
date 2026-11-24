using PES2017Editor.Binary;
using PES2017Editor.Cpk;

namespace PES2017Editor.Database;

public sealed class PesDatabaseReader
{
    public async Task<PesDatabaseSnapshot> ReadAsync(CpkIndexResult index, CpkArchiveReader reader,
        CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        var player = await ReadDocumentAsync(index, reader, "Player.bin", cancellationToken);
        var team = await ReadDocumentAsync(index, reader, "Team.bin", cancellationToken);
        var competition = await ReadDocumentAsync(index, reader, "Competition.bin", cancellationToken);
        var assignment = await ReadDocumentAsync(index, reader, "PlayerAssignment.bin", cancellationToken);

        var players = player is null ? [] : PesDatabaseParser.ParsePlayers(player.Wesys.Payload);
        var teams = team is null ? [] : PesDatabaseParser.ParseTeams(team.Wesys.Payload);
        var competitions = competition is null ? [] : PesDatabaseParser.ParseCompetitions(competition.Wesys.Payload);
        var assignments = assignment is null ? [] : PesDatabaseParser.ParseAssignments(assignment.Wesys.Payload);
        if (player is null) warnings.Add("Player.bin não foi localizado em uma TOC legível.");
        if (team is null) warnings.Add("Team.bin não foi localizado em uma TOC legível.");
        if (competition is null) warnings.Add("Competition.bin não foi localizado em uma TOC legível.");
        if (assignment is null) warnings.Add("PlayerAssignment.bin não foi localizado em uma TOC legível.");

        var playerIds = players.Select(x => x.Id).ToHashSet();
        var teamIds = teams.Select(x => x.Id).ToHashSet();
        var relationships = assignments
            .Where(x => playerIds.Contains(x.PlayerIdCandidate) && teamIds.Contains(x.TeamIdCandidate))
            .GroupBy(x => x.PlayerIdCandidate)
            .ToDictionary(x => x.Key, x => x.First().TeamIdCandidate);
        var invalid = assignments.Count(x => !playerIds.Contains(x.PlayerIdCandidate) || !teamIds.Contains(x.TeamIdCandidate));
        if (invalid > 0) warnings.Add($"{invalid:N0} associações não puderam ser resolvidas pelos IDs candidatos comprovados.");
        return new PesDatabaseSnapshot(players, teams, competitions, assignments, relationships, player, team,
            competition, assignment, warnings);
    }

    private static async Task<PesBinaryDocument?> ReadDocumentAsync(CpkIndexResult index, CpkArchiveReader reader,
        string fileName, CancellationToken cancellationToken)
    {
        var entry = index.Entries.FirstOrDefault(x =>
            Path.GetFileName(x.InternalPath).Equals(fileName, StringComparison.OrdinalIgnoreCase));
        if (entry is null) return null;
        var stored = await reader.ReadEntryAsync(entry, cancellationToken);
        var wesys = WesysCodec.Decode(stored);
        return new PesBinaryDocument(entry, stored, wesys, wesys.Payload);
    }
}
