using System;
using System.Collections.Generic;
using System.Linq;
using Statlyn.Data.Persistence;

namespace Statlyn.Data.RoleLab
{
    public sealed class RoleLabOutputProfileBridge
    {
        private readonly RoleLabRepository _repository;

        public RoleLabOutputProfileBridge(StatlynDbConnectionFactory connectionFactory)
        {
            _repository = new RoleLabRepository(connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory)));
        }

        public RoleOutputExpectationProfile? FindSelectedProfile(string selection)
        {
            if (string.IsNullOrWhiteSpace(selection))
            {
                return null;
            }

            var role = _repository.LoadRoleByName(selection.Trim());
            if (role != null)
            {
                return CreateProfileForRole(role);
            }

            var pair = _repository.LoadRolePairByName(selection.Trim());
            return pair == null ? null : CreateProfileForPair(pair);
        }

        public RoleOutputExpectationProfile CreateProfileForRole(TacticalRoleModel role)
        {
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            return CreateProfile(
                "Role Lab: " + role.RoleName,
                role.PositionGroup,
                role.RoleFamily.ToString(),
                role.TacticalPhase.ToString(),
                _repository.LoadMetricRequirementsForRole(role.Id),
                _repository.LoadScoutQuestionsForRole(role.Id),
                _repository.LoadRedFlagsForRole(role.Id));
        }

        public RoleOutputExpectationProfile CreateProfileForPair(TacticalRolePairModel pair)
        {
            if (pair == null)
            {
                throw new ArgumentNullException(nameof(pair));
            }

            var inRole = _repository.LoadRole(pair.InPossessionRoleId);
            var outRole = _repository.LoadRole(pair.OutOfPossessionRoleId);
            var pairRequirements = _repository.LoadMetricRequirementsForPair(pair.Id).ToList();
            var requirements = pairRequirements.Count > 0
                ? pairRequirements
                : _repository.LoadMetricRequirementsForRole(pair.InPossessionRoleId)
                    .Concat(_repository.LoadMetricRequirementsForRole(pair.OutOfPossessionRoleId))
                    .GroupBy(item => item.FieldName, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .ToList();
            var questions = _repository.LoadScoutQuestionsForPair(pair.Id);
            var redFlags = _repository.LoadRedFlagsForPair(pair.Id);

            return CreateProfile(
                "Role Lab Pair: " + pair.PairName,
                inRole == null ? string.Empty : inRole.PositionGroup,
                "DualPhasePair",
                TacticalPhase.DualPhasePair.ToString(),
                requirements,
                questions,
                redFlags);
        }

        private static RoleOutputExpectationProfile CreateProfile(
            string name,
            string positionGroup,
            string roleFamily,
            string tacticalPhase,
            IReadOnlyList<RoleOutputMetricRequirementModel> requirements,
            IReadOnlyList<RoleScoutQuestionModel> questions,
            IReadOnlyList<RoleRedFlagModel> redFlags)
        {
            return new RoleOutputExpectationProfile(
                name,
                positionGroup,
                roleFamily,
                tacticalPhase,
                isFm26Specific: false,
                isGenericTemplate: true,
                metricExpectations: requirements.Select(ToMetricExpectation).ToList(),
                attributeSupportWeights: "attributes=supporting evidence only",
                scoutQuestionPrompts: string.Join(" | ", (questions ?? new List<RoleScoutQuestionModel>()).Select(question => question.Question)),
                redFlagRules: string.Join(" | ", (redFlags ?? new List<RoleRedFlagModel>()).Select(flag => flag.Message)),
                minimumSampleRules: "Role Lab minimum sample rules are generic/import-ready until FM26 validation.",
                notes: "Role Lab profile bridge; FM26 validation pending and not an official mapping.");
        }

        private static MetricExpectation ToMetricExpectation(RoleOutputMetricRequirementModel requirement)
        {
            return new MetricExpectation(
                requirement.MetricKey,
                requirement.FieldName,
                requirement.Weight,
                requirement.Importance.ToString(),
                requirement.Direction.ToString(),
                requirement.MinimumSampleMinutes,
                requirement.Per90Required,
                requirement.NormalizationHint,
                requirement.EvidenceTemplate,
                requirement.MissingDataImpact);
        }
    }
}
