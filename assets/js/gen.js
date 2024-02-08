function generateQuestions(data, category, module) {

    for (var i = 0; i < data[0].length; i++) 
    {
        let questionData = data[0][i]
        let entry = '<h4 id="pytanie-' + questionData.id +'">' + questionData.id + ". " + questionData.pytanie + '</h4><div class="media"><div class="media-content"><div class="content">';
        if (questionData.ilustracja != '') {
            entry += '<p class="image"><img class="ilustration" src="../ilustracje/' + questionData.ilustracja + '"></p>'
        }
        entry += '<ol type="a"><li>' + questionData.odpa + '</li><li>' + questionData.odpb + '</li><li>' + questionData.odpc + '</li></ol>'
        entry += '<hr></div></div></div>'

        d3.select("#questions").insert("article").attr("class", "post").html(entry);

    }

}
