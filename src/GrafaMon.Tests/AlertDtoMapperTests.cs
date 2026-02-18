// Copyright(C) 2026 Simon Weston
// Licensed under the GNU General Public License v3.0
// SPDX-License-Identifier: GPL-3.0-only
using GrafaMon.Domain;
using GrafaMon.Infrastructure.Dtos;
using GrafaMon.Infrastructure.Mappers;
using NUnit.Framework;
using System;

namespace GrafaMon.Tests
{
    [TestFixture]
    public class AlertDtoMapperTests
    {
        #region Null Handling Tests

        [Test]
        public void MapToAlertDetail_WithNullDto_ThrowsArgumentNullException()
        {
            // Arrange
            GrafanaAlertDto? dto = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => AlertDtoMapper.MapToAlertDetail(dto!));
        }

        [Test]
        public void MapToAlertDetail_WithNullLabels_ReturnsUnknownValues()
        {
            // Arrange
            var dto = new GrafanaAlertDto
            {
                Labels = null,
                Annotations = null,
                UpdatedAt = "2026-01-15T10:30:00Z"
            };

            // Act
            var result = AlertDtoMapper.MapToAlertDetail(dto);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Severity, Is.EqualTo(AlertSeverity.Unknown));
                Assert.That(result.Environment, Is.EqualTo("unknown"));
                Assert.That(result.MonitorTest, Is.EqualTo("unknown"));
                Assert.That(result.Host, Is.EqualTo("unknown"));
                Assert.That(result.Summary, Is.EqualTo("No summary available"));
            });
        }

        [Test]
        public void MapToAlertDetail_WithNullAnnotations_UsesFallbackSummary()
        {
            // Arrange
            var dto = new GrafanaAlertDto
            {
                Labels = new LabelsDto
                {
                    AlertName = "TestAlert",
                    Severity = "critical"
                },
                Annotations = null,
                UpdatedAt = "2026-01-15T10:30:00Z"
            };

            // Act
            var result = AlertDtoMapper.MapToAlertDetail(dto);

            // Assert
            Assert.That(result.Summary, Is.EqualTo("TestAlert"));
        }

        [Test]
        public void MapToAlertDetail_WithAllNullFields_ReturnsValidAlertWithDefaults()
        {
            // Arrange
            var dto = new GrafanaAlertDto
            {
                Labels = null,
                Annotations = null,
                UpdatedAt = null,
                StartsAt = null,
                GeneratorUrl = null
            };

            // Act
            var result = AlertDtoMapper.MapToAlertDetail(dto);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Severity, Is.EqualTo(AlertSeverity.Unknown));
                Assert.That(result.Environment, Is.EqualTo("unknown"));
                Assert.That(result.MonitorTest, Is.EqualTo("unknown"));
                Assert.That(result.Host, Is.EqualTo("unknown"));
                Assert.That(result.Summary, Is.EqualTo("No summary available"));
                Assert.That(result.GrafanaAlertUrl, Is.EqualTo(string.Empty));

                // Check that LastUpdatedUtc is close to UtcNow (within 5 seconds)
                var timeDifference = Math.Abs((DateTime.UtcNow - result.LastUpdatedUtc).TotalSeconds);
                Assert.That(timeDifference, Is.LessThan(5), "LastUpdatedUtc should be close to UtcNow");
            });
        }

        #endregion

        #region Severity Parsing Tests

        [Test]
        [TestCase("critical", AlertSeverity.Critical)]
        [TestCase("Critical", AlertSeverity.Critical)]
        [TestCase("CRITICAL", AlertSeverity.Critical)]
        [TestCase("warning", AlertSeverity.Warning)]
        [TestCase("Warning", AlertSeverity.Warning)]
        [TestCase("WARNING", AlertSeverity.Warning)]
        [TestCase("info", AlertSeverity.Info)]
        [TestCase("Info", AlertSeverity.Info)]
        [TestCase("INFO", AlertSeverity.Info)]
        public void MapToAlertDetail_WithValidSeverity_ParsesCorrectly(string severityString, AlertSeverity expectedSeverity)
        {
            // Arrange
            var dto = new GrafanaAlertDto
            {
                Labels = new LabelsDto { Severity = severityString },
                UpdatedAt = "2026-01-15T10:30:00Z"
            };

            // Act
            var result = AlertDtoMapper.MapToAlertDetail(dto);

            // Assert
            Assert.That(result.Severity, Is.EqualTo(expectedSeverity));
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("invalid")]
        [TestCase("error")]
        [TestCase("debug")]
        public void MapToAlertDetail_WithInvalidSeverity_ReturnsUnknown(string? severityString)
        {
            // Arrange
            var dto = new GrafanaAlertDto
            {
                Labels = new LabelsDto { Severity = severityString },
                UpdatedAt = "2026-01-15T10:30:00Z"
            };

            // Act
            var result = AlertDtoMapper.MapToAlertDetail(dto);

            // Assert
            Assert.That(result.Severity, Is.EqualTo(AlertSeverity.Unknown));
        }

        #endregion

        #region DateTime Parsing Tests

        [Test]
        public void MapToAlertDetail_WithValidIso8601DateTime_ParsesCorrectly()
        {
            // Arrange
            var expectedDateTime = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);
            var dto = new GrafanaAlertDto
            {
                UpdatedAt = "2026-01-15T10:30:00Z",
                StartsAt = "2026-01-15T09:00:00Z"
            };

            // Act
            var result = AlertDtoMapper.MapToAlertDetail(dto);

            // Assert
            Assert.That(result.LastUpdatedUtc, Is.EqualTo(expectedDateTime));
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("invalid-date")]
        [TestCase("2026-13-45")]
        public void MapToAlertDetail_WithInvalidDateTime_UsesUtcNowFallback(string? dateTimeString)
        {
            // Arrange
            var dto = new GrafanaAlertDto
            {
                UpdatedAt = dateTimeString,
                StartsAt = dateTimeString
            };

            // Act
            var result = AlertDtoMapper.MapToAlertDetail(dto);

            // Assert
            var timeDifference = Math.Abs((DateTime.UtcNow - result.LastUpdatedUtc).TotalSeconds);
            Assert.That(timeDifference, Is.LessThan(5), "LastUpdatedUtc should be close to UtcNow");
        }

        [Test]
        public void MapToAlertDetail_WithNullStartsAt_UsesUpdatedAtFallback()
        {
            // Arrange
            var expectedDateTime = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);
            var dto = new GrafanaAlertDto
            {
                UpdatedAt = "2026-01-15T10:30:00Z",
                StartsAt = null
            };

            // Act
            var result = AlertDtoMapper.MapToAlertDetail(dto);

            // Assert
            Assert.That(result.LastUpdatedUtc, Is.EqualTo(expectedDateTime));
        }

        #endregion

        #region Field Fallback Tests

        [Test]
        public void MapToAlertDetail_WithMissingHost_UsesFallbackChain()
        {
            // Arrange - Host is null, but Instance is available
            var dto = new GrafanaAlertDto
            {
                Labels = new LabelsDto
                {
                    Host = null,
                    Instance = "server-01.example.com"
                },
                UpdatedAt = "2026-01-15T10:30:00Z"
            };

            // Act
            var result = AlertDtoMapper.MapToAlertDetail(dto);

            // Assert
            Assert.That(result.Host, Is.EqualTo("server-01.example.com"));
        }

        [Test]
        public void MapToAlertDetail_WithMissingHostAndInstance_ReturnsUnknown()
        {
            // Arrange
            var dto = new GrafanaAlertDto
            {
                Labels = new LabelsDto
                {
                    Host = null,
                    Instance = null
                },
                UpdatedAt = "2026-01-15T10:30:00Z"
            };

            // Act
            var result = AlertDtoMapper.MapToAlertDetail(dto);

            // Assert
            Assert.That(result.Host, Is.EqualTo("unknown"));
        }

        [Test]
        public void MapToAlertDetail_WithMissingServiceName_UsesFallbackChain()
        {
            // Arrange - ServiceName is null, but AlertName is available
            var dto = new GrafanaAlertDto
            {
                Labels = new LabelsDto
                {
                    ServiceName = null,
                    AlertName = "DatabaseConnectionAlert"
                },
                UpdatedAt = "2026-01-15T10:30:00Z"
            };

            // Act
            var result = AlertDtoMapper.MapToAlertDetail(dto);

            // Assert
            Assert.That(result.MonitorTest, Is.EqualTo("DatabaseConnectionAlert"));
        }

        [Test]
        public void MapToAlertDetail_WithMissingSummary_UsesFallbackChain()
        {
            // Arrange - Summary is null, but Description is available
            var dto = new GrafanaAlertDto
            {
                Annotations = new AnnotationsDto
                {
                    Summary = null,
                    Description = "Database connection timeout"
                },
                UpdatedAt = "2026-01-15T10:30:00Z"
            };

            // Act
            var result = AlertDtoMapper.MapToAlertDetail(dto);

            // Assert
            Assert.That(result.Summary, Is.EqualTo("Database connection timeout"));
        }

        #endregion

        #region Complete Valid Alert Tests

        [Test]
        public void MapToAlertDetail_WithCompleteValidDto_MapsAllFieldsCorrectly()
        {
            // Arrange
            var dto = new GrafanaAlertDto
            {
                Labels = new LabelsDto
                {
                    Severity = "critical",
                    Environment = "production",
                    ServiceName = "api-service",
                    Host = "prod-server-01",
                    AlertName = "HighCpuUsage"
                },
                Annotations = new AnnotationsDto
                {
                    Summary = "CPU usage above 90%",
                    Description = "Server CPU usage has exceeded threshold"
                },
                UpdatedAt = "2026-01-15T10:30:00Z",
                StartsAt = "2026-01-15T09:00:00Z",
                GeneratorUrl = "https://grafana.example.com/alerting/grafana/abc123/view",
                Fingerprint = "abc123def456"
            };

            // Act
            var result = AlertDtoMapper.MapToAlertDetail(dto);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Severity, Is.EqualTo(AlertSeverity.Critical));
                Assert.That(result.Environment, Is.EqualTo("production"));
                Assert.That(result.MonitorTest, Is.EqualTo("api-service"));
                Assert.That(result.Host, Is.EqualTo("prod-server-01"));
                Assert.That(result.Summary, Is.EqualTo("CPU usage above 90%"));
                Assert.That(result.LastUpdatedUtc, Is.EqualTo(new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc)));
                Assert.That(result.GrafanaAlertUrl, Is.EqualTo("https://grafana.example.com/alerting/grafana/abc123/view"));
            });
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void MapToAlertDetail_WithWhitespaceOnlyFields_TreatsAsEmpty()
        {
            // Arrange
            var dto = new GrafanaAlertDto
            {
                Labels = new LabelsDto
                {
                    Severity = "   ",
                    Environment = "   ",
                    ServiceName = "   ",
                    Host = "   "
                },
                Annotations = new AnnotationsDto
                {
                    Summary = "   "
                },
                UpdatedAt = "2026-01-15T10:30:00Z"
            };

            // Act
            var result = AlertDtoMapper.MapToAlertDetail(dto);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Severity, Is.EqualTo(AlertSeverity.Unknown));
                Assert.That(result.Environment, Is.EqualTo("unknown"));
                Assert.That(result.MonitorTest, Is.EqualTo("unknown"));
                Assert.That(result.Host, Is.EqualTo("unknown"));
                Assert.That(result.Summary, Is.EqualTo("No summary available"));
            });
        }

        [Test]
        public void MapToAlertDetail_WithEmptyGeneratorUrl_ReturnsEmptyString()
        {
            // Arrange
            var dto = new GrafanaAlertDto
            {
                GeneratorUrl = null,
                UpdatedAt = "2026-01-15T10:30:00Z"
            };

            // Act
            var result = AlertDtoMapper.MapToAlertDetail(dto);

            // Assert
            Assert.That(result.GrafanaAlertUrl, Is.EqualTo(string.Empty));
        }

        [Test]
        public void MapToAlertDetail_WithSeverityContainingExtraSpaces_TrimsAndParses()
        {
            // Arrange
            var dto = new GrafanaAlertDto
            {
                Labels = new LabelsDto { Severity = "  critical  " },
                UpdatedAt = "2026-01-15T10:30:00Z"
            };

            // Act
            var result = AlertDtoMapper.MapToAlertDetail(dto);

            // Assert
            Assert.That(result.Severity, Is.EqualTo(AlertSeverity.Critical));
        }

        [Test]
        public void MapToAlertDetail_WithValuesContainingExtraSpaces_TrimsValues()
        {
            // Arrange
            var dto = new GrafanaAlertDto
            {
                Labels = new LabelsDto
                {
                    Severity = "  critical  ",
                    Environment = "  production  ",
                    ServiceName = "  api-service  ",
                    Host = "  server-01  "
                },
                Annotations = new AnnotationsDto
                {
                    Summary = "  High CPU usage  "
                },
                UpdatedAt = "2026-01-15T10:30:00Z"
            };

            // Act
            var result = AlertDtoMapper.MapToAlertDetail(dto);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Severity, Is.EqualTo(AlertSeverity.Critical));
                Assert.That(result.Environment, Is.EqualTo("production"));
                Assert.That(result.MonitorTest, Is.EqualTo("api-service"));
                Assert.That(result.Host, Is.EqualTo("server-01"));
                Assert.That(result.Summary, Is.EqualTo("High CPU usage"));
            });
        }

        #endregion
    }
}