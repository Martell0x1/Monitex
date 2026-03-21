const express = require("express");

const app = express();

app.use(express.static("public"));

app.listen(3000, () => {
  console.log("Dashboard-Test is listining or port 3k");
})
