<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" encoding="utf-8" indent="yes" doctype-system="about:legacy-compat"/>

  <xsl:template match="/Conferences">
    <html lang="uk">
      <head>
        <meta charset="utf-8"/>
        <title>Наукові конференції</title>
        <style>
          body { font-family: -apple-system, "Segoe UI", Arial, sans-serif; margin: 32px; color: #3B1F2B; }
          h1 { font-size: 24px; margin-bottom: 4px; }
          p.count { color: #8A6878; margin-top: 0; }
          table { border-collapse: collapse; width: 100%; font-size: 14px; }
          th { background: #A3245A; color: #fff; text-align: left; padding: 8px 10px; }
          td { border-bottom: 1px solid #F4D7E2; padding: 7px 10px; vertical-align: top; }
          tr:nth-child(even) td { background: #FCF3F7; }
          .muted { color: #B9A0AC; }
        </style>
      </head>
      <body>
        <h1>Наукові конференції</h1>
        <p class="count">Знайдено конференцій: <xsl:value-of select="count(Conference)"/></p>
        <table>
          <tr>
            <th>Назва</th><th>Тематика</th><th>Організатор</th><th>Місто</th>
            <th>Дати</th><th>Подання до</th><th>Формат</th><th>Внесок, грн</th><th>Мова</th>
          </tr>
          <xsl:apply-templates select="Conference">
            <xsl:sort select="Dates/@start"/>
          </xsl:apply-templates>
        </table>
      </body>
    </html>
  </xsl:template>

  <xsl:template match="Conference">
    <tr>
      <td><xsl:value-of select="@title"/></td>
      <td><xsl:call-template name="value"><xsl:with-param name="v" select="@topic"/></xsl:call-template></td>
      <td><xsl:call-template name="value"><xsl:with-param name="v" select="@organizer"/></xsl:call-template></td>
      <td><xsl:call-template name="value"><xsl:with-param name="v" select="@city"/></xsl:call-template></td>
      <td>
        <xsl:call-template name="date"><xsl:with-param name="d" select="Dates/@start"/></xsl:call-template>
        <xsl:text> – </xsl:text>
        <xsl:call-template name="date"><xsl:with-param name="d" select="Dates/@end"/></xsl:call-template>
      </td>
      <td><xsl:call-template name="date"><xsl:with-param name="d" select="SubmissionDeadline"/></xsl:call-template></td>
      <td><xsl:call-template name="value"><xsl:with-param name="v" select="@format"/></xsl:call-template></td>
      <td>
        <xsl:choose>
          <xsl:when test="@fee = '0'">безкоштовно</xsl:when>
          <xsl:otherwise><xsl:call-template name="value"><xsl:with-param name="v" select="@fee"/></xsl:call-template></xsl:otherwise>
        </xsl:choose>
      </td>
      <td><xsl:call-template name="value"><xsl:with-param name="v" select="@language"/></xsl:call-template></td>
    </tr>
  </xsl:template>

  <xsl:template name="value">
    <xsl:param name="v"/>
    <xsl:choose>
      <xsl:when test="string($v)"><xsl:value-of select="$v"/></xsl:when>
      <xsl:otherwise><span class="muted">—</span></xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="date">
    <xsl:param name="d"/>
    <xsl:choose>
      <xsl:when test="string($d)">
        <xsl:value-of select="concat(substring($d, 9, 2), '.', substring($d, 6, 2), '.', substring($d, 1, 4))"/>
      </xsl:when>
      <xsl:otherwise><span class="muted">—</span></xsl:otherwise>
    </xsl:choose>
  </xsl:template>
</xsl:stylesheet>
