---
title: me:speaking
layout: page
permalink: /speaking/
article_header:
  type: overlay
  theme: dark
  background_color: '#203028'
  background_image:
    gradient: 'linear-gradient(135deg, rgba(34, 139, 87 , .4), rgba(139, 34, 139, .4))'
    src: /assets/img/cqrs.jpg
---
# Upcoming
{% for conference in site.data.upcoming %}
<div class="item">
  <div class="item__image">
    <img class="image image--lg" src="{{ conference.logo }}"/>
  </div>
  <div class="item__content">
    <div class="item__header">
      <h4>
      {% if conference.url %}
      <a href="{{ conference.url }}" target="_blank">{{ conference.name }}</a>
      {% else %}
      {{ conference.name }}
      {% endif %}
      </h4>
    </div>
    <div class="item__description">
      <p>
      {{ conference.date }} @ <b>{{ conference.location }}</b><br/>
      <ul>
      {% for talk in conference.talks %}
        <li><span><b>{{ talk.name }}</b>
        {% if talk.slides %}
        (<a href="{{ talk.slides }}" target="_blank">Slides</a>)
        {% endif %}
         {% if talk.video %}
        (<a href="{{ talk.video }}" target="_blank">Video</a>)
        {% endif %}
        </span>
        </li>
      {% endfor %}
      </ul>
      </p>
    </div>
  </div>
</div>
{% endfor %}

# Past
{% for conference in site.data.conferences %}
<div class="item">
  <div class="item__image">
    <img class="image image--lg" src="{{ conference.logo }}"/>
  </div>
  <div class="item__content">
    <div class="item__header">
      <h4>
      {% if conference.url %}
      <a href="{{ conference.url }}" target="_blank">{{ conference.name }}</a>
      {% else %}
      {{ conference.name }}
      {% endif %}
      </h4>
    </div>
    <div class="item__description">
      <p>
      {{ conference.date }} @ <b>{{ conference.location }}</b><br/>
      <ul>
      {% for talk in conference.talks %}
        <li><span><b>{{ talk.name }}</b>
        {% if talk.slides %}
        (<a href="{{ talk.slides }}" target="_blank">Slides</a>)
        {% endif %}
         {% if talk.video %}
        (<a href="{{ talk.video }}" target="_blank">Video</a>)
        {% endif %}
        </span>
        </li>
      {% endfor %}
      </ul>
      </p>
    </div>
  </div>
</div>
{% endfor %}
