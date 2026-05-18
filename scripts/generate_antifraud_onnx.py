"""Gera modelo ONNX anti-fraude (input shape [1,4], output score 0-1)."""
import os
import numpy as np
from sklearn.ensemble import RandomForestRegressor
from skl2onnx import convert_sklearn
from skl2onnx.common.data_types import FloatTensorType

OUTPUT = os.environ.get(
    "HUBPAY_ONNX_OUTPUT",
    os.path.join("src", "HubPay.WebApi", "Models", "antifraud.onnx"),
)

rng = np.random.default_rng(42)
X = rng.random((5000, 4), dtype=np.float32)
y = np.clip(
    X[:, 0] * 0.25
    + X[:, 1] * 0.15
    + X[:, 2] * 0.35
    + X[:, 3] * 0.25
    + rng.normal(0, 0.05, 5000),
    0,
    1,
).astype(np.float32)

model = RandomForestRegressor(n_estimators=50, max_depth=8, random_state=42)
model.fit(X, y)

initial_type = [("input", FloatTensorType([None, 4]))]
onnx_model = convert_sklearn(model, initial_types=initial_type, target_opset=12)

os.makedirs(os.path.dirname(OUTPUT), exist_ok=True)
with open(OUTPUT, "wb") as f:
    f.write(onnx_model.SerializeToString())

print(f"Modelo ONNX gravado em: {OUTPUT}")
