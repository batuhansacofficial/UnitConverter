window.addEventListener("DOMContentLoaded", () => {
  SwaggerUIBundle({
    url: "/openapi/v1.json",
    dom_id: "#swagger-ui",
    deepLinking: true,
    displayRequestDuration: true,
    defaultModelsExpandDepth: -1,
    validatorUrl: null,
    supportedSubmitMethods: ["get", "post"],
  });
});
