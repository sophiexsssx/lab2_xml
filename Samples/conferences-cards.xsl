<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" encoding="utf-8" indent="yes" doctype-system="about:legacy-compat"/>

  <xsl:key name="by-city" match="Conference" use="@city"/>

  <xsl:template match="/Conferences">
    <html lang="uk">
      <head>
        <meta charset="utf-8"/>
        <title>Конференції за містами</title>
        <style>
          body { font-family: Georgia, "Times New Roman", serif; background: #f3f6f4; color: #1f3329; margin: 0; padding: 32px; }
          h1 { font-size: 30px; margin: 0 0 4px; }
          .sub { color: #5b7266; margin: 0 0 28px; }
          h2 { font-size: 20px; border-bottom: 2px solid #2e7d5b; padding-bottom: 4px; margin: 28px 0 12px; }
          .cards { display: flex; flex-wrap: wrap; gap: 14px; }
          .card { background: #fff; border-radius: 10px; padding: 14px 16px; width: 280px; box-shadow: 0 1px 3px rgba(0,0,0,.12); }
          .card h3 { font-size: 16px; margin: 0 0 6px; }
          .date { color: #2e7d5b; font-weight: bold; font-size: 14px; }
          .meta { font-size: 13px; color: #5b7266; margin-top: 6px; }
          .badge { display: inline-block; font-size: 12px; padding: 2px 8px; border-radius: 10px; background: #e3f1ea; color: #2e7d5b; margin-top: 8px; }
          .free { background: #2e7d5b; color: #fff; }
        </style>
      </head>
      <body>
        <h1>Конференції за містами</h1>
        <p class="sub">Усього: <xsl:value-of select="count(Conference)"/></p>

        <xsl:for-each select="Conference[generate-id() = generate-id(key('by-city', @city)[1])]">
          <xsl:sort select="@city"/>
          <h2><xsl:value-of select="@city"/> (<xsl:value-of select="count(key('by-city', @city))"/>)</h2>
          <div class="cards">
            <xsl:for-each select="key('by-city', @city)">
              <xsl:sort select="Dates/@start"/>
              <xsl:apply-templates select="."/>
            </xsl:for-each>
          </div>
        </xsl:for-each>
      </body>
    </html>
  </xsl:template>

  <xsl:template match="Conference">
    <div class="card">
      <h3><xsl:value-of select="@title"/></h3>
      <div class="date">
        <xsl:value-of select="concat(substring(Dates/@start, 9, 2), '.', substring(Dates/@start, 6, 2), ' – ',
                                     substring(Dates/@end, 9, 2), '.', substring(Dates/@end, 6, 2), '.', substring(Dates/@end, 1, 4))"/>
      </div>
      <div class="meta"><xsl:value-of select="@topic"/> · <xsl:value-of select="@organizer"/></div>
      <xsl:if test="@format"><span class="badge"><xsl:value-of select="@format"/></span><xsl:text> </xsl:text></xsl:if>
      <xsl:choose>
        <xsl:when test="@fee = '0'"><span class="badge free">безкоштовно</span></xsl:when>
        <xsl:when test="@fee"><span class="badge"><xsl:value-of select="@fee"/> грн</span></xsl:when>
      </xsl:choose>
    </div>
  </xsl:template>
</xsl:stylesheet>
