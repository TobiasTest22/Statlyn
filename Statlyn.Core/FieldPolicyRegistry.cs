using System;
using System.Collections.Generic;

namespace Statlyn.Core
{
    public sealed class FieldPolicyRegistry
    {
        private readonly Dictionary<PlayerFieldKey, FieldPolicy> _policies;

        public FieldPolicyRegistry()
        {
            _policies = new Dictionary<PlayerFieldKey, FieldPolicy>();
            RegisterDefaults();
        }

        public IReadOnlyDictionary<PlayerFieldKey, FieldPolicy> Policies
        {
            get { return _policies; }
        }

        public FieldPolicy GetPolicy(PlayerFieldKey key)
        {
            if (key == PlayerFieldKey.Unknown)
            {
                return FieldPolicy.Denied(PlayerFieldKey.Unknown, "Unknown fields are denied by default.");
            }

            return _policies.TryGetValue(key, out var policy)
                ? policy
                : FieldPolicy.Denied(key, "No field policy is registered for this field.");
        }

        public PlayerFieldKey ResolveKey(string rawName, PlayerFieldKey declaredKey)
        {
            var normalized = Normalize(rawName);
            var forbidden = ResolveForbiddenName(normalized);
            if (forbidden != PlayerFieldKey.Unknown)
            {
                return forbidden;
            }

            if (declaredKey != PlayerFieldKey.Unknown)
            {
                return declaredKey;
            }

            switch (normalized)
            {
                case "sourceplayerid":
                    return PlayerFieldKey.SourcePlayerId;
                case "displayname":
                case "name":
                case "playername":
                    return PlayerFieldKey.DisplayName;
                case "age":
                    return PlayerFieldKey.Age;
                case "dateofbirth":
                case "dob":
                    return PlayerFieldKey.DateOfBirth;
                case "nationality":
                    return PlayerFieldKey.Nationality;
                case "nationalityflag":
                case "flag":
                    return PlayerFieldKey.NationalityFlag;
                case "playerface":
                case "playerfaceimage":
                case "faceimage":
                case "image":
                    return PlayerFieldKey.PlayerFaceImage;
                case "club":
                    return PlayerFieldKey.Club;
                case "league":
                    return PlayerFieldKey.League;
                case "primaryposition":
                case "position":
                    return PlayerFieldKey.PrimaryPosition;
                case "secondarypositions":
                    return PlayerFieldKey.SecondaryPositions;
                case "preferredfoot":
                case "foot":
                    return PlayerFieldKey.PreferredFoot;
                case "height":
                    return PlayerFieldKey.Height;
                case "weight":
                    return PlayerFieldKey.Weight;
                case "contractend":
                case "contractexpiry":
                    return PlayerFieldKey.ContractEnd;
                case "wage":
                    return PlayerFieldKey.Wage;
                case "marketvalue":
                case "value":
                    return PlayerFieldKey.MarketValue;
                case "estimatedvaluerange":
                    return PlayerFieldKey.EstimatedValueRange;
                case "availability":
                    return PlayerFieldKey.Availability;
                case "transferstatus":
                    return PlayerFieldKey.TransferStatus;
                case "loanstatus":
                    return PlayerFieldKey.LoanStatus;
                case "interestlevel":
                case "interest":
                    return PlayerFieldKey.InterestLevel;
                case "technicalattribute":
                    return PlayerFieldKey.TechnicalAttribute;
                case "physicalattribute":
                    return PlayerFieldKey.PhysicalAttribute;
                case "playerstat":
                case "stat":
                    return PlayerFieldKey.PlayerStat;
                case "physicaldata":
                    return PlayerFieldKey.PhysicalData;
                case "scoutobservation":
                    return PlayerFieldKey.ScoutObservation;
                case "clubbadge":
                case "clubbadgeimage":
                    return PlayerFieldKey.ClubBadge;
                case "scoutknowledge":
                    return PlayerFieldKey.ScoutKnowledge;
                case "scoutstars":
                    return PlayerFieldKey.ScoutStars;
                case "scoutrecommendation":
                case "scoutverdict":
                    return PlayerFieldKey.ScoutRecommendation;
                case "visiblepersonalitynote":
                case "scoutvisiblepersonalitynote":
                case "personalitynote":
                    return PlayerFieldKey.ScoutVisiblePersonalityNote;
                case "licensedexternaldata":
                    return PlayerFieldKey.LicensedExternalData;
                default:
                    return PlayerFieldKey.Unknown;
            }
        }

        public bool IsForbiddenRawName(string rawName)
        {
            return ResolveForbiddenName(Normalize(rawName)) != PlayerFieldKey.Unknown;
        }

        private void RegisterDefaults()
        {
            Register(PlayerFieldKey.SourcePlayerId, FieldVisibilityCategory.AlwaysVisible, true, false, true);
            Register(PlayerFieldKey.DisplayName, FieldVisibilityCategory.AlwaysVisible, true, false, true);
            Register(PlayerFieldKey.Age, FieldVisibilityCategory.AlwaysVisible, true, false, true);
            Register(PlayerFieldKey.DateOfBirth, FieldVisibilityCategory.AlwaysVisible, true, false, true);
            Register(PlayerFieldKey.Nationality, FieldVisibilityCategory.AlwaysVisible, true, false, true);
            Register(PlayerFieldKey.NationalityFlag, FieldVisibilityCategory.AlwaysVisible, true, false, true);
            Register(PlayerFieldKey.PlayerFaceImage, FieldVisibilityCategory.LicensedExternalData, true, false, true, requiresLicensedSource: true);
            Register(PlayerFieldKey.Club, FieldVisibilityCategory.AlwaysVisible, true, false, true);
            Register(PlayerFieldKey.League, FieldVisibilityCategory.AlwaysVisible, true, false, true);
            Register(PlayerFieldKey.PrimaryPosition, FieldVisibilityCategory.AlwaysVisible, true, true, true);
            Register(PlayerFieldKey.SecondaryPositions, FieldVisibilityCategory.AlwaysVisible, true, true, true);
            Register(PlayerFieldKey.PreferredFoot, FieldVisibilityCategory.VisibleIfScouted, true, true, true, requiresScoutReport: true, minimumScoutKnowledge: 30);
            Register(PlayerFieldKey.Height, FieldVisibilityCategory.AlwaysVisible, true, false, true);
            Register(PlayerFieldKey.Weight, FieldVisibilityCategory.VisibleIfScouted, true, false, true, requiresScoutReport: true, minimumScoutKnowledge: 30);
            Register(PlayerFieldKey.ContractEnd, FieldVisibilityCategory.VisibleIfScouted, true, true, true, requiresScoutReport: true, minimumScoutKnowledge: 20);
            Register(PlayerFieldKey.Wage, FieldVisibilityCategory.VisibleAsEstimateOnly, true, true, true, requiresScoutReport: true, minimumScoutKnowledge: 40);
            Register(PlayerFieldKey.MarketValue, FieldVisibilityCategory.VisibleAsEstimateOnly, true, true, true, requiresScoutReport: true, minimumScoutKnowledge: 30);
            Register(PlayerFieldKey.EstimatedValueRange, FieldVisibilityCategory.VisibleAsEstimateOnly, true, true, true, requiresScoutReport: true, minimumScoutKnowledge: 30);
            Register(PlayerFieldKey.Availability, FieldVisibilityCategory.VisibleIfScouted, true, true, true, requiresScoutReport: true, minimumScoutKnowledge: 30);
            Register(PlayerFieldKey.TransferStatus, FieldVisibilityCategory.VisibleIfScouted, true, true, true, requiresScoutReport: true, minimumScoutKnowledge: 30);
            Register(PlayerFieldKey.LoanStatus, FieldVisibilityCategory.VisibleIfScouted, true, true, true, requiresScoutReport: true, minimumScoutKnowledge: 30);
            Register(PlayerFieldKey.InterestLevel, FieldVisibilityCategory.VisibleIfScouted, true, true, true, requiresScoutReport: true, minimumScoutKnowledge: 30);
            Register(PlayerFieldKey.TechnicalAttribute, FieldVisibilityCategory.VisibleIfScouted, true, true, true, requiresScoutReport: true, minimumScoutKnowledge: 50);
            Register(PlayerFieldKey.PhysicalAttribute, FieldVisibilityCategory.VisibleIfScouted, true, true, true, requiresScoutReport: true, minimumScoutKnowledge: 50);
            Register(PlayerFieldKey.PlayerStat, FieldVisibilityCategory.LicensedExternalData, true, true, true, requiresLicensedSource: true);
            Register(PlayerFieldKey.PhysicalData, FieldVisibilityCategory.LicensedExternalData, true, true, true, requiresLicensedSource: true);
            Register(PlayerFieldKey.ScoutObservation, FieldVisibilityCategory.VisibleIfScouted, true, true, true);
            Register(PlayerFieldKey.ScoutKnowledge, FieldVisibilityCategory.VisibleIfScouted, true, true, true, requiresScoutReport: true, minimumScoutKnowledge: 1);
            Register(PlayerFieldKey.ScoutStars, FieldVisibilityCategory.VisibleIfScouted, true, true, true, requiresScoutReport: true, minimumScoutKnowledge: 1);
            Register(PlayerFieldKey.ScoutRecommendation, FieldVisibilityCategory.VisibleIfScouted, true, true, true, requiresScoutReport: true, minimumScoutKnowledge: 1);
            Register(PlayerFieldKey.ScoutVisiblePersonalityNote, FieldVisibilityCategory.VisibleIfScouted, true, false, true, requiresScoutReport: true, minimumScoutKnowledge: 1);
            Register(PlayerFieldKey.UserNote, FieldVisibilityCategory.UserEnteredNote, true, false, true);
            Register(PlayerFieldKey.LicensedExternalData, FieldVisibilityCategory.LicensedExternalData, true, true, true, requiresLicensedSource: true);
            Register(PlayerFieldKey.ClubBadge, FieldVisibilityCategory.LicensedExternalData, true, false, true, requiresLicensedSource: true);

            RegisterForbidden(PlayerFieldKey.CurrentAbility, "Hidden FM26 Current Ability is never visible, stored or scored.");
            RegisterForbidden(PlayerFieldKey.PotentialAbility, "Hidden FM26 Potential Ability is never visible, stored or scored.");
            RegisterForbidden(PlayerFieldKey.HiddenPersonality, "Hidden personality values are never visible, stored or scored.");
            RegisterForbidden(PlayerFieldKey.InjuryProneness, "Hidden injury proneness is never visible, stored or scored.");
            RegisterForbidden(PlayerFieldKey.Consistency, "Hidden consistency is never visible, stored or scored.");
            RegisterForbidden(PlayerFieldKey.ImportantMatches, "Hidden important matches is never visible, stored or scored.");
            RegisterForbidden(PlayerFieldKey.Professionalism, "Hidden professionalism is never visible, stored or scored.");
            RegisterForbidden(PlayerFieldKey.Pressure, "Hidden pressure is never visible, stored or scored.");
            RegisterForbidden(PlayerFieldKey.Ambition, "Hidden ambition is never visible, stored or scored.");
            RegisterForbidden(PlayerFieldKey.Loyalty, "Hidden loyalty is never visible, stored or scored.");
            RegisterForbidden(PlayerFieldKey.Adaptability, "Hidden adaptability is never visible, stored or scored.");
            RegisterForbidden(PlayerFieldKey.Temperament, "Hidden temperament is never visible, stored or scored.");
        }

        private void Register(
            PlayerFieldKey key,
            FieldVisibilityCategory category,
            bool canDisplay,
            bool canScore,
            bool canStore,
            bool requiresScoutReport = false,
            int minimumScoutKnowledge = 0,
            bool requiresLicensedSource = false)
        {
            _policies[key] = new FieldPolicy(
                key,
                category,
                canDisplay,
                canScore,
                canStore,
                requiresScoutReport,
                minimumScoutKnowledge,
                requiresLicensedSource,
                isFm26HiddenValue: false,
                missingReason: "Field is not available from the current provider or scouting state.");
        }

        private void RegisterForbidden(PlayerFieldKey key, string reason)
        {
            _policies[key] = new FieldPolicy(
                key,
                FieldVisibilityCategory.NeverVisible,
                canDisplay: false,
                canScore: false,
                canStore: false,
                requiresScoutReport: false,
                minimumScoutKnowledge: 0,
                requiresLicensedSource: false,
                isFm26HiddenValue: true,
                missingReason: reason);
        }

        private static string Normalize(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return string.Empty;
            }

            var chars = new List<char>();
            foreach (var character in rawName)
            {
                if (char.IsLetterOrDigit(character))
                {
                    chars.Add(char.ToLowerInvariant(character));
                }
            }

            return new string(chars.ToArray());
        }

        private static PlayerFieldKey ResolveForbiddenName(string normalized)
        {
            switch (normalized)
            {
                case "ca":
                case "currentability":
                    return PlayerFieldKey.CurrentAbility;
                case "pa":
                case "potentialability":
                    return PlayerFieldKey.PotentialAbility;
                case "hiddenpersonality":
                case "personality":
                    return PlayerFieldKey.HiddenPersonality;
                case "injuryproneness":
                case "injurypronenness":
                    return PlayerFieldKey.InjuryProneness;
                case "consistency":
                    return PlayerFieldKey.Consistency;
                case "importantmatches":
                    return PlayerFieldKey.ImportantMatches;
                case "professionalism":
                    return PlayerFieldKey.Professionalism;
                case "pressure":
                    return PlayerFieldKey.Pressure;
                case "ambition":
                    return PlayerFieldKey.Ambition;
                case "loyalty":
                    return PlayerFieldKey.Loyalty;
                case "adaptability":
                    return PlayerFieldKey.Adaptability;
                case "temperament":
                    return PlayerFieldKey.Temperament;
                default:
                    return PlayerFieldKey.Unknown;
            }
        }
    }
}
