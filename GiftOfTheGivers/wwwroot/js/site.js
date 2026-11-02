// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Confirmation prompts for destructive form buttons: <button data-confirm="Are you sure?">.
// Keeps user-provided text (e.g. project titles) out of inline JavaScript.
document.addEventListener('submit', function (event) {
  var button = event.submitter;
  var message = button && button.getAttribute('data-confirm');
  if (message && !window.confirm(message)) {
    event.preventDefault();
  }
});
