using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Voting;
using Content.Server.Voting.Managers;
using Content.Shared.GameTicking;
using Content.Shared.Maps;
using Content.Shared._Nivalis.Planets;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Nivalis.Planets;

public sealed class NivalisPlanetVoteSystem : EntitySystem
{
    [Dependency] private readonly IGameMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IVoteManager _voteManager = default!;
    [Dependency] private readonly GameTicker _ticker = default!;

    private bool _voteCreated;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_ticker.RunLevel != GameRunLevel.PreRoundLobby)
        {
            _voteCreated = false;
            return;
        }

        if (_voteCreated)
            return;

        if (_ticker.Preset?.ID != "NivalisSurvivor")
            return;

        _voteCreated = true;
        StartPlanetVote();
    }

    private void StartPlanetVote()
    {
        var planets = new Dictionary<string, NivalisPlanetPrototype>();
        foreach (var planet in _proto.EnumeratePrototypes<NivalisPlanetPrototype>())
        {
            if (!IsPlanetVotable(planet))
                continue;

            planets[planet.MapId.Id] = planet;
        }

        if (planets.Count == 0)
        {
            _chat.DispatchGlobalAnnouncement(Loc.GetString("nivalis-planet-vote-empty"));
            return;
        }

        var options = new VoteOptions
        {
            Title = Loc.GetString("nivalis-planet-vote-title"),
            InitiatorText = Loc.GetString("ui-vote-initiator-server"),
            Duration = TimeSpan.FromSeconds(30),
        };

        foreach (var (_, planet) in planets)
        {
            options.Options.Add((Loc.GetString(planet.Name), planet.MapId.Id));
        }

        var vote = _voteManager.CreateVote(options);
        vote.OnFinished += (_, args) =>
        {
            string pickedId;
            if (args.Winner == null)
            {
                pickedId = (string) _random.Pick(args.Winners);
                _chat.DispatchGlobalAnnouncement(
                    Loc.GetString("nivalis-planet-vote-tie", ("picked", Loc.GetString(planets[pickedId].Name))));
            }
            else
            {
                pickedId = (string) args.Winner;
                _chat.DispatchGlobalAnnouncement(
                    Loc.GetString("nivalis-planet-vote-win", ("winner", Loc.GetString(planets[pickedId].Name))));
            }

            SelectWinner(pickedId);
        };
    }

    private bool IsPlanetVotable(NivalisPlanetPrototype planet)
    {
        if (!_proto.TryIndex<EntityPrototype>(planet.PlanetEntity, out var entity))
            return false;

        // The planet's map entity must be part of the Nivalis planet pool.
        if (!entity.HasComp<NivalisPlanetPoolComponent>(_componentFactory))
            return false;

        return _mapManager.CheckMapExists(planet.MapId.Id);
    }

    private void SelectWinner(string mapId)
    {
        if (!_ticker.CanUpdateMap() || !_mapManager.CheckMapExists(mapId))
            return;

        _mapManager.SelectMap(mapId);
        _ticker.UpdateInfoText();
    }
}
